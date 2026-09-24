using DMP.BL.Constants;
using DMP.BL.Factories;
using DMP.BL.Models;
using DMP.BL.Models.Enumerations;
using DMP.DataAccess;
using DMP.DataAccess.Models;
using DMP.DataAccess.Models.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace DMP.BL.Services;

public class MenuService(
    IMenuFactory menuFactory,
    ITranslationService translationService,
    IRedisService redisService,
    IDbContextFactory<DmpDbContext> dmpContextFactory) : IMenuService
{
    private const int PagingSize = 7;

    public async Task<Menu?> GetMenu(string lng, int menuId)
    {
        var cacheKey = string.Format(CacheKeys.MenuKey, menuId, lng);
        var result = await redisService.GetCacheValue<Menu>(cacheKey);

        if (result is not null)
        {
            return result;
        }

        result = await GenerateMenu(menuId, lng);

        await redisService.SetCacheValue(cacheKey, result);

        return result;
    }

    public async Task<Menu?> GenerateMenu(int menuId, string lng)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var menu = await context.Menus
            .Include(m => m.Categories!.OrderBy(c => c.Order))
            .AsNoTracking()
            .FirstOrDefaultAsync(mc => mc.MenuId == menuId);

        if (menu is null)
        {
            return null;
        }

        var result = menuFactory.ConvertMenuToBL(menu);

        if (result.Categories is not null)
        {
            result.Categories = await TranslateMenuCategories(result.Categories, lng);
        }

        return result;
    }

    public async Task<MenuCategory?> GetMenuCategory(string lng, int categoryId)
    {
        var cacheKey = string.Format(CacheKeys.MenuCategoryKey, categoryId, lng);
        var result = await redisService.GetCacheValue<MenuCategory>(cacheKey);

        if (result is not null)
        {
            return result;
        }

        var category = await GetMenuCategoryDAL(categoryId);
        result = await ConvertMenuCategoryToBL(category, lng);

        await redisService.SetCacheValue(cacheKey, result);

        return result;
    }

    public string FindRootCategory(List<MenuCategoryDAL> cats, int? id)
    {
        var cat = cats.Find(c => c.MenuCategoryId == id)!;
        return cat.ParentId.HasValue ? FindRootCategory(cats, cat.ParentId) : cat.MenuCategoryId.ToString();
    }

    public async Task<List<MenuCategoryDAL>> GetAllCategoriesDal()
    {
        var result = await redisService.GetCacheValue<List<MenuCategoryDAL>?>(CacheKeys.DALAllMenuCategoriesKey);

        if (result is not null)
        {
            return result;
        }

        result = await GenerateAllCategoriesDal();

        await redisService.SetCacheValue(CacheKeys.DALAllMenuCategoriesKey, result);

        return result;
    }

    public async Task<List<MenuCategoryDAL>> GenerateAllCategoriesDal()
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        return await context.MenuCategories
            .Include(i => i.Children)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<MenuCategoryData?> GetCategoryData(string lng, int categoryId,
        string categorySlug, Dictionary<string, string> filters, int page = 1, short pageSize = 12)
    {
        var getCategoryDataInfoTask = GetMenuCategory(lng, categoryId);

        var getProductsTask = Task.Run(async () =>
        {
            var allCategoryIds = await GetAllCategoryIds(categoryId);
            if (allCategoryIds is null)
            {
                return (null, default);
            }

            var cats = await GetAllCategoriesDal();

            var ranges = new Dictionary<string, (string min, string max)>();
            ExtractRange(filters, "price", ranges);
            ExtractRange(filters, "duration", ranges);
            ExtractRange(filters, "year", ranges);

            var sorting = ProductSorting.New;
            if (filters.Remove("sorting", out var sortingStr)
                && Enum.TryParse<ProductSorting>(sortingStr, ignoreCase: true, out var sortingParsed))
            {
                sorting = sortingParsed;
            }

            var searchParams = new ProductSearchParams
            {
                CategoryId = string.Join("|", allCategoryIds),
                RootCategoryId = FindRootCategory(cats, categoryId),
                PagingSize = PagingSize,
                Page = page,
                PageSize = pageSize,
                Filters = filters,
                Ranges = ranges.Count > 0 ? ranges : default,
                SortBy = sorting
            };

            return await redisService.SearchProducts(searchParams);
        });

        await Task.WhenAll(getCategoryDataInfoTask, getProductsTask);

        var menuCategoryDataInfo = await getCategoryDataInfoTask;
        var (products, totalCount) = await getProductsTask;

        if (menuCategoryDataInfo is null || !menuCategoryDataInfo.Url.Contains(categorySlug))
        {
            return null;
        }

        return new MenuCategoryData
        {
            Category = menuCategoryDataInfo,
            Products = products?.ToList(),
            TotalCount = totalCount
        };
    }

    public async Task<IEnumerable<Product?>?> GetRecommendation()
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var productIds = await context.Products.AsNoTracking().Where(p => p.Status == ProductStatus.Ready)
            .OrderBy(p => p.ProductId).Take(30).Select(p => p.ProductId).ToArrayAsync();

        return await redisService.SearchProducts(productIds);
    }

    public async Task<IEnumerable<Product>?> GetRecommendationByCategory(int categoryId)
    {
        var allCategoryIds = await GetAllCategoryIds(categoryId);
        if (allCategoryIds is null)
        {
            return null;
        }

        var cats = await GetAllCategoriesDal();

        var searchParams = new ProductSearchParams
        {
            CategoryId = string.Join("|", allCategoryIds),
            RootCategoryId = FindRootCategory(cats, categoryId),
            PagingSize = PagingSize,
            Page = 1,
            PageSize = 5
        };

        var (products, _) = await redisService.SearchProducts(searchParams);

        return products;
    }

    /// <summary>Moves a "min;max" range filter from <paramref name="filters"/> into <paramref name="ranges"/>.</summary>
    private static void ExtractRange(Dictionary<string, string> filters, string key,
        Dictionary<string, (string min, string max)> ranges)
    {
        if (!filters.Remove(key, out var rangeStr))
        {
            return;
        }

        var parts = rangeStr.Split(';');
        var maxValue = parts.Length > 1 ? parts[1] : "+inf";
        ranges.Add(key, (parts[0], maxValue));
    }

    public async Task<MenuCategory?> ConvertMenuCategoryToBL(MenuCategoryDAL? category, string lng)
    {
        if (category is null)
        {
            return null;
        }

        var result = menuFactory.ConvertMenuCategoryToBL(category);

        result.Title = await translationService.GetMenuTranslation(true, result.MenuCategoryId, result.Name, lng);
        if (result.Children is not null)
        {
            result.Children = await TranslateMenuCategories(result.Children, lng);
        }

        if (category.Parent is not null)
        {
            result.Parent = await ConvertMenuCategoryToBL(category.Parent, lng);
        }

        var commonFeature = await GetCommonFeature();
        List<CategoryFeature> featuresList = [await ConvertFeatureToBl(commonFeature!, lng)];

        if (category.CategoryFeatures is not null)
        {
            foreach (var feature in category.CategoryFeatures)
            {
                featuresList.Add(await ConvertFeatureToBl(feature.Feature!, lng));
            }
        }

        result.Features = featuresList;

        return result;
    }

    public async Task<CategoryFeature> ConvertFeatureToBl(FeatureDAL feature, string lng)
    {
        var featureTranslationKey = string.Format(TranslationKeys.FeatureKey, feature.FeatureId, feature.Name);
        var title = await translationService.GetTranslation(featureTranslationKey, lng);

        var blFeature = new CategoryFeature
        {
            FeatureId = feature.FeatureId,
            Name = feature.Name,
            Title = title ?? feature.Name,
            Type = feature.Type
        };

        switch (feature.Type)
        {
            case FeatureType.Bool:
                break;
            case FeatureType.Checkboxes:
                blFeature.FilterCheckboxes = [];
                foreach (var featureCheckboxesValue in feature.CheckboxesValues!)
                {
                    var featureItemTranslationKey = string.Format(TranslationKeys.FeatureItemKey, feature.FeatureId, featureCheckboxesValue);
                    var itemTitle = await translationService.GetTranslation(featureItemTranslationKey, lng);
                    blFeature.FilterCheckboxes.Add(new CategoryFeatureCheckboxItem
                    {
                        Key = featureCheckboxesValue,
                        Title = itemTitle ?? featureCheckboxesValue
                    });
                }

                blFeature.MultipleChoice = feature.MultipleChoice;
                break;
            case FeatureType.Range:
                blFeature.RangeMin = feature.RangeMin;
                blFeature.RangeMax = feature.RangeMax;
                blFeature.RangeFrom = feature.RangeFrom;
                blFeature.RangeTo = feature.RangeTo;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(feature), feature.Type, "Unknown feature type.");
        }

        return blFeature;
    }

    /// <summary>The "price" feature is shared by every category.</summary>
    private async Task<FeatureDAL?> GetCommonFeature()
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        return await context.Features.AsNoTracking().FirstOrDefaultAsync(f => f.Name == "price");
    }

    private async Task<MenuCategoryDAL?> GetMenuCategoryDAL(int categoryId)
    {
        var cacheKey = string.Format(CacheKeys.DALMenuCategoryKey, categoryId);
        var result = await redisService.GetCacheValue<MenuCategoryDAL>(cacheKey);

        if (result is not null)
        {
            return result;
        }

        result = await GenerateMenuCategoryDAL(categoryId);

        await redisService.SetCacheValue(cacheKey, result);

        return result;
    }

    public async Task<MenuCategoryDAL?> GenerateMenuCategoryDAL(int categoryId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();

        return await context.MenuCategories
            .Include(mc => mc.Children!.OrderBy(c => c.Order))
            .ThenInclude(mc => mc.Children!.OrderBy(c => c.Order))
            .Include(mc => mc.Parent)
            .ThenInclude(mcp => mcp!.Parent)
            .Include(mc => mc.CategoryFeatures!)
            .ThenInclude(cf => cf.Feature)
            .AsNoTracking()
            .FirstOrDefaultAsync(mc => mc.MenuCategoryId == categoryId);
    }

    private async Task<List<int>?> GetAllCategoryIds(int categoryId)
    {
        var cacheKey = string.Format(CacheKeys.MenuCategoryChildIdsKey, categoryId);
        var result = await redisService.GetCacheValue<List<int>>(cacheKey);

        if (result is not null)
        {
            return result;
        }

        result = await GenerateAllCategoryIds(categoryId);

        if (result is null)
        {
            return null;
        }

        await redisService.SetCacheValue(cacheKey, result);

        return result;
    }

    public async Task<List<int>?> GenerateAllCategoryIds(int categoryId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        return context.GetAllCategoryIds(categoryId);
    }

    private async Task<IEnumerable<MenuCategory>> TranslateMenuCategories(IEnumerable<MenuCategory> categories,
        string lng)
    {
        var translateMenuCategories = categories.ToList();
        foreach (var menuCategory in translateMenuCategories)
        {
            menuCategory.Title = await translationService.GetMenuTranslation(true, menuCategory.MenuCategoryId,
                menuCategory.Name, lng);

            if (menuCategory.Children is not null)
            {
                menuCategory.Children = await TranslateMenuCategories(menuCategory.Children, lng);
            }
        }

        return translateMenuCategories;
    }
}
