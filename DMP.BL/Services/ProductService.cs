using DMP.BL.Models;
using DMP.BL.Models.Search;
using DMP.BL.Models.Store;
using DMP.DataAccess;
using DMP.DataAccess.Models;
using DMP.DataAccess.Models.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace DMP.BL.Services;

public class ProductService(
    IRedisService redisService,
    IDbContextFactory<DmpDbContext> dmpContextFactory,
    IMenuService menuService) : IProductService
{
    public async Task<ProductData?> GetProductData(string lng, int productId, string productSlug)
    {
        var (product, _) = await redisService.SearchProduct(productId);

        if (product is null || !string.Equals(product.Slug, productSlug, StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        var category = await menuService.GetMenuCategory(lng, int.Parse(product.MenuCategoryId));

        if (category is null)
        {
            return null;
        }

        return new ProductData
        {
            Product = product,
            Category = category,
        };
    }

    public Task<IEnumerable<Product?>> GetProducts(int[] ids) => redisService.SearchProducts(ids);

    public async Task<Product?> GetProduct(int id) => (await redisService.SearchProduct(id)).product;

    public async Task UpdateCacheProducts(List<int> productIds)
    {
        // Products are cached by dmp.job.server, which processes these queued tasks.
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        context.JobProductCacheTasks.AddRange(productIds.Select(id => new JobProductCacheTaskDAL { ProductId = id }));
        await context.SaveChangesAsync();
    }

    public async Task RemoveProductsFromCache(List<ProductDAL> products)
    {
        var catsForRoot = await menuService.GetAllCategoriesDal();

        foreach (var product in products)
        {
            var rootId = menuService.FindRootCategory(catsForRoot, product.MenuCategoryId);
            redisService.RemoveProduct(product.ProductId, rootId);
        }
    }

    public async Task<FullTextSearchProductsResponse> SearchProducts(string query, string lng)
    {
        var searchResult = await redisService.FullTextSearch(query, lng);
        await SetFeaturesToSearchProducts(searchResult.Products, lng);

        return searchResult;
    }

    public async Task<FullTextSearchResponse> FullTextSearchProducts(string query, PaginationModel pagination, string lng)
    {
        var pageSize = pagination.PageSize ?? 10;
        var offset = (pagination.Page - 1) * pageSize;
        var (products, totalCount) = await redisService.FullTextSearchProducts(query, offset, pageSize);

        await SetFeaturesToSearchProducts(products, lng);

        return new FullTextSearchResponse
        {
            Products = products,
            TotalCount = totalCount
        };
    }

    public async Task<string> GetProductFeaturesValues(Product product, string lng)
    {
        var category = await menuService.GetMenuCategory(lng, int.Parse(product.MenuCategoryId));

        var featuresValues = new List<string>();
        foreach (var productFeature in product.Features)
        {
            var feature = category!.Features!.First(cf => cf.Name == productFeature.Key);
            if (feature.Type == FeatureType.Checkboxes)
            {
                featuresValues.Add(GetCheckboxTitle(feature, productFeature.Value));
            }
        }

        return string.Join(", ", featuresValues);
    }

    public async Task<List<Product?>> GetFavoritesProducts(int[] productIds) =>
        [.. await redisService.SearchProducts(productIds)];

    public async Task<ProductStoreInfo?> GetProductStoreInfo(int productId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var storeDal = await context.Products.AsNoTracking().Where(p => p.ProductId == productId)
            .Join(context.Stores.AsNoTracking(), p => p.SellerId, s => s.SellerId, (_, store) => store)
            .FirstOrDefaultAsync();

        if (storeDal is null)
        {
            return null;
        }

        return new ProductStoreInfo
        {
            StoreId = storeDal.SellerId,
            Name = storeDal.Name,
            CoverPath = storeDal.CoverPath,
        };
    }

    /// <summary>Replaces checkbox feature keys with their translated titles.</summary>
    private async Task SetFeaturesToSearchProducts(List<Product>? products, string lng)
    {
        if (products is null)
        {
            return;
        }

        var categories = new List<MenuCategory>();
        foreach (var menuCategoryId in products.Select(p => p.MenuCategoryId).Distinct())
        {
            var category = await menuService.GetMenuCategory(lng, int.Parse(menuCategoryId));
            if (category is not null)
            {
                categories.Add(category);
            }
        }

        foreach (var product in products)
        {
            var category = categories.Find(c => c.MenuCategoryId.ToString() == product.MenuCategoryId);
            foreach (var productFeature in product.Features)
            {
                var feature = category!.Features!.First(cf => cf.Name == productFeature.Key);
                if (feature.Type == FeatureType.Checkboxes)
                {
                    productFeature.Value = GetCheckboxTitle(feature, productFeature.Value);
                }
            }
        }
    }

    private static string GetCheckboxTitle(CategoryFeature feature, string? value) =>
        feature.FilterCheckboxes!.First(fc => feature.Name + "_" + fc.Key == value).Title;
}
