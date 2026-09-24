using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Security.Cryptography;
using DMP.BL.Constants;
using DMP.BL.Factories;
using DMP.BL.Helpers;
using DMP.BL.Models;
using DMP.BL.Models.Enumerations;
using DMP.BL.Models.ProductCreation;
using DMP.DataAccess;
using DMP.DataAccess.Models;
using DMP.DataAccess.Models.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMP.BL.Services;

public class ProductManagerService(
    ILogger<ProductManagerService> logger,
    IDbContextFactory<DmpDbContext> dmpContextFactory,
    IMenuFactory menuFactory,
    ITranslationService translationService,
    IRedisService redisService,
    IProductService productService,
    IProductImagesService productImagesService,
    IProductFileStorageService productFileStorageService,
    IMenuService menuService) : IProductManagerService
{
    private const int PresignedUrlExpiry = 60 * 60 * 24; // 24h
    private const long MaxProductFileSize = 1L * 1024 * 1024 * 1024; // 1GB per file
    private const long MaxUserStorage = 1L * 1024 * 1024 * 1024; // 1GB per user

    private readonly ProductStatus[] _editableStatuses = [ProductStatus.New, ProductStatus.NeedWork, ProductStatus.PausedByUser];

    public async Task<GetNewProductResponse> GetProduct(Guid userId, int productId)
    {
        var result = new GetNewProductResponse();

        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var productDal = await context.Products
            .Where(p => p.ProductId == productId && p.SellerId == userId && _editableStatuses.Contains(p.Status))
            .Include(p => p.ProductCreation)
            .Include(p => p.ProductFeatures)
            .Include(p => p.ProductImages!.Where(pi => pi.Attached))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (productDal?.ProductCreation is null)
        {
            result.Status = GetNewProductRequestStatus.Error;
            return result;
        }

        var productCreation = productDal.ProductCreation;

        result.Status = GetNewProductRequestStatus.Success;
        result.Product = new NewProductRequest
        {
            ProductId = productDal.ProductId,
            MenuCategoryId = productCreation.UseCustomCategory ? null : productDal.MenuCategoryId,
            UseCustomCategory = productCreation.UseCustomCategory,
            CustomCategory = productCreation.UseCustomCategory ? productCreation.CustomCategory : null,
            PreviousStatus = productCreation.PreviousStatus,
            UserFeatures = new UserFeature
            {
                Name = productDal.UserFeature!.Name,
                Description = productDal.UserFeature.Description
            },
            Features = productDal.ProductFeatures?.ToDictionary(pf => pf.FeatureId, pf => pf.Value),
            Price = productDal.Price,
            ProductImages = productDal.ProductImages?.Select(ToNewProductImage)
        };

        return result;
    }

    public async Task<ProductView?> GetProductView(Guid userId, int productId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var productDal = await context.Products.Where(p => p.ProductId == productId && p.SellerId == userId)
            .Include(p => p.ProductCreation)
            .Include(p => p.ProductFeatures)
            .Include(p => p.ProductImages!.Where(pi => pi.Attached))
            .Include(p => p.ProductFiles!.Where(pf => pf.Attached && pf.IsUploaded))
            .Include(p => p.ProductLines)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (productDal is null)
        {
            return null;
        }

        var productView = new ProductView
        {
            ProductId = productDal.ProductId,
            MenuCategoryId = productDal.MenuCategoryId,
            UserFeatures = new UserFeature
            {
                Name = productDal.UserFeature!.Name,
                Description = productDal.UserFeature.Description
            },
            Features = productDal.ProductFeatures?.ToDictionary(pf => pf.FeatureId, pf => pf.Value),
            Price = productDal.Price,
            ProductImages = productDal.ProductImages?.Select(ToNewProductImage)
        };

        if (productDal.ProductCreation is { } productCreation)
        {
            productView.MenuCategoryId = productCreation.UseCustomCategory ? null : productDal.MenuCategoryId;
            productView.UseCustomCategory = productCreation.UseCustomCategory;
            productView.CustomCategory = productCreation.UseCustomCategory ? productCreation.CustomCategory : null;
            productView.PreviousStatus = productCreation.PreviousStatus;
            productView.MessageForSeller = productCreation.MessageForSeller;
        }

        var userStorageUsed = await GetUserStorageUsed(context, userId);
        productView.ProductDataItems = SetProductDataToModel(userStorageUsed, productDal);

        return productView;
    }

    public async Task<GetProductDataItemsResponse> GetProductDataItems(Guid userId, int productId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var product = await context.Products.AsNoTracking()
            .Where(p => p.ProductId == productId && p.SellerId == userId && _editableStatuses.Contains(p.Status))
            .Include(p => p.ProductFiles!.Where(pi => pi.IsUploaded))
            .Include(p => p.ProductCreation)
            .FirstOrDefaultAsync();

        if (product is null || (!product.IsLines && product.ProductFiles is null))
        {
            return new GetProductDataItemsResponse();
        }

        var userStorageUsed = await GetUserStorageUsed(context, userId);

        return SetProductDataToModel(userStorageUsed, product);
    }

    public async Task<IEnumerable<ProductCategory>> GetProductCategories(string lng, int menuId)
    {
        var cacheKey = string.Format(CacheKeys.ProductCategoriesKey, menuId, lng);
        var cacheResult = await redisService.GetCacheValue<ProductCategory[]>(cacheKey);

        if (cacheResult is not null)
        {
            return cacheResult;
        }

        var translatedResult = (await GenerateProductCategories(lng, menuId)).ToList();

        await redisService.SetCacheValue(cacheKey, translatedResult);

        return translatedResult;
    }

    /// <summary>Builds the category tree from the database (bypasses the cache).</summary>
    public async Task<IEnumerable<ProductCategory>> GenerateProductCategories(string lng, int menuId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var fullMenu = await context.MenuCategories.AsNoTracking().ToListAsync();

        var result = new List<ProductCategory>();

        foreach (var highLevel in fullMenu.Where(m => m.MenuId == menuId))
        {
            FormProductCategories(fullMenu, highLevel);
            result.Add(menuFactory.ConvertMenuCategoryToProductCategoryBL(highLevel));
        }

        return (await TranslateProductCategories(result, lng)).ToList();
    }

    public async Task<IEnumerable<CategoryFeature>> GetCategoryFeatures(int categoryId, string lng)
    {
        var cacheKey = string.Format(CacheKeys.CategoryFeaturesKey, categoryId, lng);
        var cacheResult = await redisService.GetCacheValue<CategoryFeature[]>(cacheKey);

        if (cacheResult is not null)
        {
            return cacheResult;
        }

        var result = await GenerateCategoryFeatures(categoryId, lng);

        await redisService.SetCacheValue(cacheKey, result);

        return result;
    }

    /// <summary>Loads the category features from the database (bypasses the cache).</summary>
    public async Task<List<CategoryFeature>> GenerateCategoryFeatures(int categoryId, string lng)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var features = await context.CategoryFeatures.Where(cf => cf.CategoryId == categoryId).AsNoTracking()
            .Select(cf => cf.Feature).ToListAsync();

        var result = new List<CategoryFeature>();

        foreach (var feature in features)
        {
            result.Add(await menuService.ConvertFeatureToBl(feature!, lng));
        }

        return result;
    }

    public async Task<CreateOrUpdateProductResponse> CreateOrUpdateNewProduct(Guid userId,
        NewProductRequest newProduct, string lng)
    {
        var result = new CreateOrUpdateProductResponse();

        await using var dbContext = await dmpContextFactory.CreateDbContextAsync();

        var newProductFeatures = new List<ProductFeatureDAL>();
        if (!newProduct.UseCustomCategory)
        {
            var categoryFeatures = await GetCategoryFeatures(newProduct.MenuCategoryId!.Value, lng);
            foreach (var categoryFeature in categoryFeatures)
            {
                // Every category feature is required and must hold a valid value.
                if (!newProduct.Features!.TryGetValue(categoryFeature.FeatureId, out var featureItem)
                    || string.IsNullOrEmpty(featureItem))
                {
                    result.Status = CreateOrUpdateProductStatus.InternalError;
                    return result;
                }

                var featureValue = ParseFeatureValue(categoryFeature, featureItem);
                if (string.IsNullOrEmpty(featureValue))
                {
                    result.Status = CreateOrUpdateProductStatus.InternalError;
                    return result;
                }

                newProductFeatures.Add(new ProductFeatureDAL
                {
                    FeatureId = categoryFeature.FeatureId,
                    Value = featureValue
                });
            }
        }

        ProductDAL? productDal;
        if (newProduct.ProductId.HasValue)
        {
            productDal = await dbContext.Products.FirstOrDefaultAsync(p =>
                p.ProductId == newProduct.ProductId && p.SellerId == userId && _editableStatuses.Contains(p.Status));

            if (productDal is null)
            {
                result.Status = CreateOrUpdateProductStatus.InternalError;
                return result;
            }
        }
        else
        {
            productDal = new ProductDAL
            {
                Unlimited = true
            };
        }

        productDal.MenuCategoryId = newProduct.UseCustomCategory ? 0 : newProduct.MenuCategoryId!.Value;
        productDal.UserFeature = new UserFeatureDAL
        {
            Description = newProduct.UserFeatures.Description,
            Name = newProduct.UserFeatures.Name,
        };
        productDal.Slug = SlugHelper.ToSlug(newProduct.UserFeatures.Name);
        productDal.SellerId = userId;
        productDal.Price = newProduct.Price;
        productDal.Status = ProductStatus.New;

        if (!newProduct.ProductId.HasValue)
        {
            dbContext.Add(productDal);

            await SendSysNotification($"User added new product.\nUserId: {userId}");
        }
        else
        {
            dbContext.Update(productDal);
        }

        await dbContext.SaveChangesAsync();
        var productId = productDal.ProductId;

        var productCreationDb = await dbContext.ProductCreations.FirstOrDefaultAsync(pc => pc.ProductId == productId);
        if (productCreationDb is not null)
        {
            productCreationDb.CustomCategory = newProduct.CustomCategory;
            productCreationDb.UseCustomCategory = newProduct.UseCustomCategory;
            dbContext.Update(productCreationDb);
        }
        else
        {
            dbContext.ProductCreations.Add(new ProductCreationDAL
            {
                ProductId = productId,
                CustomCategory = newProduct.CustomCategory,
                UseCustomCategory = newProduct.UseCustomCategory
            });
        }

        await dbContext.SaveChangesAsync();

        await dbContext.ProductFeatures.Where(pf => pf.ProductId == productId).ExecuteDeleteAsync();
        if (newProductFeatures.Count > 0)
        {
            newProductFeatures.ForEach(pf => pf.ProductId = productId);

            dbContext.ProductFeatures.AddRange(newProductFeatures);
        }

        await dbContext.SaveChangesAsync();

        result.ProductId = productId;
        result.Status = CreateOrUpdateProductStatus.Success;

        return result;
    }

    public async Task<UploadProductImageResponse> UploadProductImage(Guid userId, NewProductFile imageFile, int productId)
    {
        var result = new UploadProductImageResponse();

        await using var dbContext = await dmpContextFactory.CreateDbContextAsync();

        var productExists = await dbContext.Products.AnyAsync(p =>
            p.ProductId == productId && p.SellerId == userId && _editableStatuses.Contains(p.Status));

        if (!productExists)
        {
            result.Status = CreateOrUpdateProductStatus.InternalError;
            return result;
        }

        var imgHash = ComputeFileHash(imageFile.Data);

        var existingImage = await dbContext.ProductImages.AsNoTracking()
            .FirstOrDefaultAsync(pi => pi.ProductId == productId && pi.Hash == imgHash);

        if (existingImage is not null)
        {
            result.Status = CreateOrUpdateProductStatus.Success;
            result.Image = ToNewProductImage(existingImage);

            return result;
        }

        var (status, savedImage) = await productImagesService.SaveProductImage(imageFile, productId);

        if (status != CreateOrUpdateProductStatus.Success)
        {
            result.Status = status;
            return result;
        }

        var productImage = new ProductImageDAL
        {
            ProductId = productId,
            Hash = imgHash,
            Images = new StoredImageDAL
            {
                Original = savedImage!.StoredPath
            },
            Name = savedImage.FileName
        };

        dbContext.ProductImages.Add(productImage);
        await dbContext.SaveChangesAsync();

        result.Status = CreateOrUpdateProductStatus.Success;
        result.Image = new NewProductImage
        {
            Hash = productImage.Hash,
            ImgUrl = productImage.Images.Original
        };

        return result;
    }

    public async Task<bool> SetProductImages(Guid userId, NewProductImage[]? newProductImages, int productId)
    {
        await using var dbContext = await dmpContextFactory.CreateDbContextAsync();

        var productDal = await dbContext.Products.AsNoTracking().Include(p => p.ProductImages).FirstOrDefaultAsync(p =>
            p.ProductId == productId && p.SellerId == userId && _editableStatuses.Contains(p.Status));

        if (productDal is null)
        {
            return false;
        }

        // Detached images are only unlinked from the product; the files stay in S3.
        var needSave = false;
        if ((newProductImages is null || newProductImages.Length == 0) && productDal.ProductImages is not null)
        {
            foreach (var productImageDal in productDal.ProductImages.Where(pi => pi.Attached))
            {
                productImageDal.Attached = false;
                productImageDal.IsCover = false;
                dbContext.ProductImages.Update(productImageDal);
                needSave = true;
            }
        }
        else
        {
            foreach (var productImageDal in productDal.ProductImages!)
            {
                var newProductImage = newProductImages!.FirstOrDefault(pi => pi.Hash == productImageDal.Hash);
                if (newProductImage is null)
                {
                    productImageDal.Attached = false;
                    productImageDal.IsCover = false;
                    dbContext.ProductImages.Update(productImageDal);
                    needSave = true;
                    continue;
                }

                if (newProductImage.IsCover || productImageDal.IsCover)
                {
                    productImageDal.IsCover = newProductImage.IsCover;
                    dbContext.ProductImages.Update(productImageDal);
                    needSave = true;
                }

                if (!productImageDal.Attached)
                {
                    productImageDal.Attached = true;
                    dbContext.ProductImages.Update(productImageDal);
                    needSave = true;
                }
            }
        }

        if (needSave)
        {
            await dbContext.SaveChangesAsync();
        }

        return true;
    }

    public async Task<GenerateUploadUrlResponse> GenerateUploadUrl(Guid userId, GenerateUploadUrlRequest request)
    {
        var result = new GenerateUploadUrlResponse();

        await using var dbContext = await dmpContextFactory.CreateDbContextAsync();
        var productExists = await dbContext.Products.AnyAsync(p =>
            p.ProductId == request.ProductId && p.SellerId == userId && _editableStatuses.Contains(p.Status));

        if (!productExists)
        {
            logger.LogError("User {UserId} tried to upload a file for an invalid product {ProductId}", userId, request.ProductId);

            result.Status = GenerateUploadUrlStatus.InternalError;
            return result;
        }

        var userStorageUsed = await GetUserStorageUsed(dbContext, userId);

        if (userStorageUsed + request.FileSize > MaxUserStorage)
        {
            logger.LogInformation(
                "User {UserId} exceeded the storage limit. File size: {FileSize}, used: {StorageUsed}, productId: {ProductId}",
                userId, request.FileSize, userStorageUsed, request.ProductId);

            result.Status = GenerateUploadUrlStatus.StorageLimitExceeded;
            return result;
        }

        if (request.FileSize > MaxProductFileSize)
        {
            logger.LogInformation(
                "User {UserId} exceeded the file size limit. File size: {FileSize}, used: {StorageUsed}, productId: {ProductId}",
                userId, request.FileSize, userStorageUsed, request.ProductId);

            result.Status = GenerateUploadUrlStatus.StorageLimitExceeded;
            return result;
        }

        var storageFileName = $"{Ulid.NewUlid()}-{request.FileName}";

        string presignedProductFileUrl;
        try
        {
            presignedProductFileUrl = await productFileStorageService.GetFileUploadPresignedUrl(PresignedUrlExpiry,
                request.ProductId, storageFileName);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to generate upload URL for product {ProductId}", request.ProductId);

            result.Status = GenerateUploadUrlStatus.InternalError;
            return result;
        }

        var productFile = new ProductFileDAL
        {
            ProductId = request.ProductId,
            UserId = userId,
            FileSize = request.FileSize,
            FileName = request.FileName,
            StoragePath = presignedProductFileUrl,
            IsUploaded = false
        };

        dbContext.ProductFiles.Add(productFile);
        await dbContext.SaveChangesAsync();

        result.Status = GenerateUploadUrlStatus.Success;
        result.ProductFileId = productFile.ProductFileId;
        result.PresignedUrl = presignedProductFileUrl;

        return result;
    }

    public Task<bool> DeleteProductFile(Guid userId, SetProductFileUploadedRequest request) =>
        UpdateProductFile(userId, request, productFile =>
        {
            productFile.Attached = false;
            productFile.IsUploaded = false;
        });

    public Task<bool> SetProductFileUploaded(Guid userId, SetProductFileUploadedRequest request) =>
        UpdateProductFile(userId, request, productFile => productFile.IsUploaded = true);

    public async Task<bool> SetProductFiles(Guid userId, SetProductFilesRequest request)
    {
        await using var dbContext = await dmpContextFactory.CreateDbContextAsync();

        var productDal = await dbContext.Products
            .Include(p => p.ProductFiles)
            .Include(p => p.ProductCreation)
            .FirstOrDefaultAsync(p =>
                p.ProductId == request.ProductId && p.SellerId == userId && _editableStatuses.Contains(p.Status));

        if (productDal is null)
        {
            return false;
        }

        var productCreation = productDal.ProductCreation!;
        if (productCreation.PreviousStatus is ProductStatus.PausedByUser)
        {
            return false;
        }

        if (!request.IsLines)
        {
            foreach (var productFile in productDal.ProductFiles!)
            {
                productFile.Attached = request.ProductFileIds?.Contains(productFile.ProductFileId) == true;
                productFile.IsUploaded = productFile.Attached;
                dbContext.ProductFiles.Update(productFile);
            }

            if (productCreation.Lines is not null)
            {
                productCreation.Lines = null;
                dbContext.ProductCreations.Update(productCreation);
            }

            productDal.Unlimited = request.IsUnlimited;
            productDal.Quantity = request.IsUnlimited ? 0 : (int)request.Quantity!;
        }
        else
        {
            var lines = request.Lines!.Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
            if (lines.Length == 0)
            {
                return false;
            }

            if (productDal.ProductFiles is not null)
            {
                foreach (var productFile in productDal.ProductFiles)
                {
                    productFile.Attached = false;
                    productFile.IsUploaded = false;
                    dbContext.ProductFiles.Update(productFile);
                }
            }

            productCreation.Lines = lines;
            dbContext.ProductCreations.Update(productCreation);

            productDal.Quantity = lines.Length;
        }

        productDal.IsLines = request.IsLines;

        await dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> FinishProductCreation(Guid userId, int productId)
    {
        await using var dbContext = await dmpContextFactory.CreateDbContextAsync();

        var product = await dbContext.Products
            .AsNoTracking()
            .Include(p => p.ProductCreation)
            .Include(p => p.ProductFiles)
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.SellerId == userId && _editableStatuses.Contains(p.Status));

        // Fast validation of the product before changing its status.
        if (product?.ProductCreation is null)
        {
            return false;
        }

        if (product.Unlimited && product.ProductFiles is null)
        {
            return false;
        }

        if (product.IsLines)
        {
            if (product.Quantity < 1 || product.Unlimited)
            {
                return false;
            }

            // A product resumed from pause keeps its already published lines in ProductLines.
            var hasLines = product.ProductCreation.PreviousStatus == ProductStatus.PausedByUser
                ? await dbContext.ProductLines.AnyAsync(pl => pl.ProductId == product.ProductId)
                : product.ProductCreation.Lines is not null;

            if (!hasLines)
            {
                return false;
            }
        }

        if (!product.Unlimited && product.Quantity < 1)
        {
            return false;
        }

        product.Status = ProductStatus.Built;
        dbContext.Products.Update(product);
        await dbContext.SaveChangesAsync();

        await SendSysNotification($"User finished product creation.\nUserId: {userId}\nProductId: {product.ProductId}");

        return true;
    }

    public async Task<GetProductListResponse> GetProductList(Guid userId, GetProductListRequest request)
    {
        var pageSize = request.Pagination.PageSize ?? 10;

        var result = new GetProductListResponse();

        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var productsQuery = context.Products.AsNoTracking()
            .Where(p => p.SellerId == userId);

        if (request.Filters is { Count: > 0 })
        {
            var firstFilter = request.Filters.First();
            productsQuery = productsQuery.Where(c => c.UserFeature != null && c.UserFeature.Name.Contains(firstFilter.Value));
        }

        result.TotalCount = await productsQuery.CountAsync();

        if (request.Sorting is { Count: > 0 })
        {
            var orderByClauses = new List<string>();

            foreach (var sortModel in request.Sorting)
            {
                if (Enum.TryParse(sortModel.Field, true, out ProductSortFields productSort))
                {
                    Enum.TryParse(sortModel.Sort, true, out SortDirection sortDirection);
                    orderByClauses.Add(sortDirection == SortDirection.asc ? productSort.ToString() : $"{productSort} {sortDirection}");
                }
                else
                {
                    logger.LogError("Cannot parse sorting field {SortField} for model {Model}", sortModel.Field, nameof(ProductDAL));
                }
            }

            orderByClauses.Add("ProductId desc");

            productsQuery = productsQuery.OrderBy(string.Join(", ", orderByClauses));
        }
        else
        {
            productsQuery = productsQuery.OrderByDescending(p => p.ProductId);
        }

        var products = await productsQuery
            .Include(p => p.ProductImages!.Where(pi => pi.Attached && pi.IsCover))
            .Include(p => p.ProductCreation)
            .Skip(request.Pagination.Page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        result.Products = products.Select(p => new ProductListItem
        {
            ProductId = p.ProductId,
            MenuCategoryId = p.MenuCategoryId,
            Quantity = p.Quantity,
            Unlimited = p.Unlimited,
            Price = p.Price,
            Name = p.UserFeature!.Name,
            Slug = p.Slug,
            ImgLink = p.ProductImages?.FirstOrDefault()?.Images.Original,
            Status = p.Status,
            CreatedAt = p.CreatedAt,
            MessageForSeller = p.ProductCreation?.MessageForSeller
        }).ToList();

        return result;
    }

    public async Task<bool> ChangeProductStatus(Guid userId, ChangeProductStatusRequest request)
    {
        // Statuses the product may currently be in for the requested transition.
        ProductStatus[]? allowedCurrentStatuses = request.NewStatus switch
        {
            ProductStatus.New => [ProductStatus.PausedByUser, ProductStatus.NeedWork],
            ProductStatus.Ready => [ProductStatus.PausedByUser],
            ProductStatus.PausedByUser => [ProductStatus.Ready],
            ProductStatus.Inactive => _editableStatuses,
            _ => null
        };

        if (allowedCurrentStatuses is null)
        {
            return false;
        }

        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var product = await context.Products.FirstOrDefaultAsync(p =>
            p.ProductId == request.ProductId && p.SellerId == userId && allowedCurrentStatuses.Contains(p.Status));

        if (product is null)
        {
            return false;
        }

        if (request.NewStatus == ProductStatus.New && product.Status == ProductStatus.PausedByUser)
        {
            context.ProductCreations.Add(new ProductCreationDAL
            {
                ProductId = product.ProductId,
                UseCustomCategory = false,
                PreviousStatus = product.Status,
            });
        }

        product.Status = request.NewStatus;
        context.Products.Update(product);

        await context.SaveChangesAsync();

        switch (request.NewStatus)
        {
            case ProductStatus.New:
            case ProductStatus.PausedByUser:
                await productService.RemoveProductsFromCache([product]);
                break;
            case ProductStatus.Ready:
                await productService.UpdateCacheProducts([product.ProductId]);
                break;
        }

        return true;
    }

    private async Task<bool> UpdateProductFile(Guid userId, SetProductFileUploadedRequest request,
        Action<ProductFileDAL> update)
    {
        await using var dbContext = await dmpContextFactory.CreateDbContextAsync();

        var productExists = await dbContext.Products.AnyAsync(p =>
            p.ProductId == request.ProductId && p.SellerId == userId && _editableStatuses.Contains(p.Status));

        if (!productExists)
        {
            return false;
        }

        var productFile = await dbContext.ProductFiles.FirstOrDefaultAsync(pf =>
            pf.ProductFileId == request.ProductFileId && pf.ProductId == request.ProductId && pf.UserId == userId);

        if (productFile is null)
        {
            return false;
        }

        update(productFile);
        dbContext.ProductFiles.Update(productFile);
        await dbContext.SaveChangesAsync();

        return true;
    }

    private static Task<long> GetUserStorageUsed(DmpDbContext context, Guid userId) =>
        context.ProductFiles.Where(f => f.UserId == userId && f.IsUploaded).SumAsync(f => f.FileSize);

    private static GetProductDataItemsResponse SetProductDataToModel(long userStorageUsed, ProductDAL product) =>
        new()
        {
            FreeSpace = BlConstants.UserSpaceVolume - userStorageUsed,
            Files = product.ProductFiles?.Select(productFile => new GetProductFilesResponse
            {
                FileName = productFile.FileName,
                FileSize = productFile.FileSize,
                ProductFileId = productFile.ProductFileId
            }).ToList(),
            Quantity = product.Quantity,
            IsLines = product.IsLines,
            IsUnlimited = product.Unlimited,
            Lines = product.IsLines ? product.ProductCreation?.Lines : null
        };

    private static NewProductImage ToNewProductImage(ProductImageDAL productImage) =>
        new()
        {
            Hash = productImage.Hash,
            ImgUrl = productImage.Images.Original,
            IsCover = productImage.IsCover
        };

    /// <returns>The normalized value to store, or <c>null</c> when <paramref name="rawValue"/> is invalid.</returns>
    private static string? ParseFeatureValue(CategoryFeature categoryFeature, string rawValue)
    {
        switch (categoryFeature.Type)
        {
            case FeatureType.Bool:
                return bool.TryParse(rawValue, out var boolValue) ? boolValue.ToString().ToLower() : null;
            case FeatureType.Checkboxes:
                var checkboxes = categoryFeature.FilterCheckboxes!;
                if (categoryFeature.MultipleChoice == true)
                {
                    // Stored as "key1;key2;" - unknown keys are dropped.
                    return string.Concat(rawValue.Split(';')
                        .Where(choice => checkboxes.Any(fc => fc.Key == choice))
                        .Select(choice => choice + ";"));
                }

                return checkboxes.FirstOrDefault(fc => fc.Key == rawValue)?.Key;
            case FeatureType.Range:
                return decimal.TryParse(rawValue, out var numValue)
                       && numValue >= categoryFeature.RangeMin && numValue <= categoryFeature.RangeMax
                    ? numValue.ToString(CultureInfo.InvariantCulture).ToLower()
                    : null;
            default:
                throw new ArgumentOutOfRangeException(nameof(categoryFeature), categoryFeature.Type, "Unknown feature type.");
        }
    }

    private async Task SendSysNotification(string message)
    {
        try
        {
            await redisService.AddSysNotification(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send system notification: {Message}", message);
        }
    }

    private static void FormProductCategories(List<MenuCategoryDAL> menuCategories, MenuCategoryDAL parent)
    {
        var children = menuCategories.Where(mc => mc.ParentId == parent.MenuCategoryId).ToList();
        foreach (var child in children)
        {
            FormProductCategories(menuCategories, child);
        }

        parent.Children = children;
    }

    private async Task<IEnumerable<ProductCategory>> TranslateProductCategories(IEnumerable<ProductCategory> categories,
        string lng)
    {
        var translateMenuCategories = categories.ToList();
        foreach (var menuCategory in translateMenuCategories)
        {
            menuCategory.Name = await translationService.GetMenuTranslation(true, menuCategory.CategoryId,
                menuCategory.Name, lng);

            if (menuCategory.Children is not null)
            {
                menuCategory.Children = await TranslateProductCategories(menuCategory.Children, lng);
            }
        }

        return translateMenuCategories;
    }

    private static string ComputeFileHash(byte[] fileData) => Convert.ToHexStringLower(MD5.HashData(fileData));
}
