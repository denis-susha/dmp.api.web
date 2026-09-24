namespace DMP.BL.Constants;

internal static class CacheKeys
{
    // Catalog
    public const string MenuKey = "catalog:menu:{0}:{1}"; // {0} - menu id, {1} - language
    public const string MenuCategoryKey = "catalog:menu:category:{0}:{1}"; // {0} - category id, {1} - language
    public const string MenuCategoryChildIdsKey = "catalog:menu:category:{0}:childrenIds"; // {0} - category id
    public const string DALMenuCategoryKey = "dal:catalog:menu:category:{0}"; // {0} - category id
    public const string DALAllMenuCategoriesKey = "dal:catalog:menu:categories";

    public const string SearchCategoryJsonKey = "search:category:{0}:{1}"; // {0} - category id, {1} - language

    // Application settings
    public const string ApplicationSettingsBitcart = "settings:application:bitcart";

    // User
    public const string UserRefreshToken = "user:{0}:refreshtoken{1}"; // {0} - user id, {1} - refresh token

    // Product manager
    public const string ProductCategoriesKey = "productcategories:{0}:{1}"; // {0} - menu id, {1} - language
    public const string CategoryFeaturesKey = "categoryfeatures:{0}:{1}"; // {0} - category id, {1} - language

    // Translation
    public const string PageModelKey = "page:{0}:{1}"; // {0} - language, {1} - translation key
}
