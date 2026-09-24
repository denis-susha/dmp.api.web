namespace DMP.BL.Services;

public interface ICacheManagerService
{
    Task ReloadAll();
    Task ReloadMenus();
    Task ReloadAllCategoriesDal();
    Task ReloadMenuCategoriesDAL();
    Task ReloadMenuCategories();
    Task ReloadGenerateAllCategoryIds();
    Task CacheCategories();
    Task ReloadAllProducts();
    Task ReloadCategoryFeaturesForProductManager();
    Task ReloadProductCategories();
}
