using DMP.BL.Constants;
using DMP.BL.Models.Search;
using DMP.DataAccess;
using DMP.DataAccess.Models.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMP.BL.Services;

public class CacheManagerService(
    ILogger<CacheManagerService> logger,
    IRedisService redisService,
    IDbContextFactory<DmpDbContext> dmpContextFactory,
    IMenuService menuService,
    IProductService productService,
    IApplicationSettingsService applicationSettingsService,
    IProductManagerService productManagerService) : ICacheManagerService
{
    private const int ProductBatchSize = 100;

    public async Task ReloadAll()
    {
        // Order matters: later steps read the caches built by earlier ones.
        await ReloadBitcartSettings();

        await ReloadAllCategoriesDal();
        await ReloadMenuCategoriesDAL();
        await ReloadMenus();
        await ReloadMenuCategories();
        await ReloadGenerateAllCategoryIds();
        await CacheCategories();
        await ReloadAllProducts();
        await ReloadCategoryFeaturesForProductManager();
        await ReloadProductCategories();
    }

    public async Task ReloadBitcartSettings()
    {
        var bitcartSettings = await applicationSettingsService.GenerateBitcartSettings();

        await redisService.SetCacheValue(CacheKeys.ApplicationSettingsBitcart, bitcartSettings);
    }

    public async Task ReloadMenus()
    {
        var menuIds = await GetMenuIds();

        foreach (var lng in BlConstants.SupportedLanguages)
        {
            foreach (var menuId in menuIds)
            {
                var cacheKey = string.Format(CacheKeys.MenuKey, menuId, lng);
                var menu = await menuService.GenerateMenu(menuId, lng);

                await redisService.SetCacheValue(cacheKey, menu);
            }
        }
    }

    public async Task ReloadAllCategoriesDal()
    {
        var result = await menuService.GenerateAllCategoriesDal();

        await redisService.SetCacheValue(CacheKeys.DALAllMenuCategoriesKey, result);
    }

    public async Task ReloadMenuCategoriesDAL()
    {
        var allCategoriesDal = await menuService.GenerateAllCategoriesDal();

        foreach (var categoryDal in allCategoriesDal)
        {
            var cacheKey = string.Format(CacheKeys.DALMenuCategoryKey, categoryDal.MenuCategoryId);
            var result = await menuService.GenerateMenuCategoryDAL(categoryDal.MenuCategoryId);

            await redisService.SetCacheValue(cacheKey, result);
        }
    }

    public async Task ReloadMenuCategories()
    {
        var allCategoriesDal = await menuService.GenerateAllCategoriesDal();

        foreach (var lng in BlConstants.SupportedLanguages)
        {
            foreach (var menuCategoryId in allCategoriesDal.Select(m => m.MenuCategoryId))
            {
                var cacheKey = string.Format(CacheKeys.MenuCategoryKey, menuCategoryId, lng);

                var category = await menuService.GenerateMenuCategoryDAL(menuCategoryId);
                var result = await menuService.ConvertMenuCategoryToBL(category, lng);

                await redisService.SetCacheValue(cacheKey, result);
            }
        }
    }

    public async Task ReloadGenerateAllCategoryIds()
    {
        var allCategoriesDal = await menuService.GenerateAllCategoriesDal();

        foreach (var menuCategoryId in allCategoriesDal.Select(m => m.MenuCategoryId))
        {
            var cacheKey = string.Format(CacheKeys.MenuCategoryChildIdsKey, menuCategoryId);
            var result = await menuService.GenerateAllCategoryIds(menuCategoryId);

            await redisService.SetCacheValue(cacheKey, result);
        }
    }

    public async Task CacheCategories()
    {
        try
        {
            await using var context = await dmpContextFactory.CreateDbContextAsync();
            var cats = await context.MenuCategories.AsNoTracking().ToListAsync();
            var catsForRoot = await menuService.GetAllCategoriesDal();

            var searchCategories = new Dictionary<(int, string), SearchCategory>();

            foreach (var cat in cats)
            {
                var rootCatId = int.Parse(menuService.FindRootCategory(catsForRoot, cat.MenuCategoryId));
                var rootCat = catsForRoot.First(cfr => cfr.MenuCategoryId == rootCatId);

                foreach (var language in BlConstants.SupportedLanguages)
                {
                    var rootCatBl = await menuService.ConvertMenuCategoryToBL(rootCat, language);
                    var catBl = await menuService.ConvertMenuCategoryToBL(cat, language);

                    searchCategories.Add((catBl!.MenuCategoryId, language), new SearchCategory
                    {
                        Title = catBl.Title,
                        RootTitle = rootCatBl!.Title,
                        Url = catBl.Url,
                        Language = language
                    });
                }
            }

            redisService.SetSearchCategories(searchCategories);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cache search categories");
            throw;
        }
    }

    public async Task ReloadAllProducts()
    {
        var catsForRoot = await menuService.GetAllCategoriesDal();

        await using var context = await dmpContextFactory.CreateDbContextAsync();

        for (var page = 0; ; page++)
        {
            var batch = await context.Products
                .Where(p => p.Status == ProductStatus.Ready && (p.Unlimited || p.Quantity > 0))
                .AsNoTracking()
                .OrderBy(x => x.ProductId)
                .Skip(page * ProductBatchSize)
                .Take(ProductBatchSize)
                .Select(p => p.ProductId)
                .ToListAsync();

            if (batch.Count == 0)
            {
                break;
            }

            await productService.UpdateCacheProducts(batch);
        }

        foreach (var rootCat in catsForRoot.Where(c => !c.ParentId.HasValue))
        {
            redisService.EnsureProductCatIndex(rootCat.MenuCategoryId.ToString());
        }

        redisService.EnsureProductIndex();
        redisService.EnsureProductSearchIndex();
    }

    public async Task ReloadCategoryFeaturesForProductManager()
    {
        var allCategoriesDal = await menuService.GenerateAllCategoriesDal();

        foreach (var lng in BlConstants.SupportedLanguages)
        {
            foreach (var menuCategoryId in allCategoriesDal.Select(m => m.MenuCategoryId))
            {
                var cacheKey = string.Format(CacheKeys.CategoryFeaturesKey, menuCategoryId, lng);
                var result = await productManagerService.GenerateCategoryFeatures(menuCategoryId, lng);

                await redisService.SetCacheValue(cacheKey, result);
            }
        }
    }

    public async Task ReloadProductCategories()
    {
        var menuIds = await GetMenuIds();

        foreach (var lng in BlConstants.SupportedLanguages)
        {
            foreach (var menuId in menuIds)
            {
                var productCategories = await productManagerService.GenerateProductCategories(lng, menuId);

                var cacheKey = string.Format(CacheKeys.ProductCategoriesKey, menuId, lng);
                await redisService.SetCacheValue(cacheKey, productCategories.ToList());
            }
        }
    }

    private async Task<List<short>> GetMenuIds()
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        return await context.Menus.Select(m => m.MenuId).ToListAsync();
    }
}
