using DMP.BL.Models;
using DMP.DataAccess.Models;

namespace DMP.BL.Services;

public interface IMenuService
{
    string FindRootCategory(List<MenuCategoryDAL> cats, int? id);
    Task<Menu?> GetMenu(string lng, int menuId);
    Task<MenuCategory?> GetMenuCategory(string lng, int categoryId);

    Task<MenuCategoryData?> GetCategoryData(string lng, int categoryId, string categorySlug,
        Dictionary<string, string> filters, int page = 1, short pageSize = 12);

    Task<List<MenuCategoryDAL>> GetAllCategoriesDal();
    Task<MenuCategory?> ConvertMenuCategoryToBL(MenuCategoryDAL? category, string lng);
    Task<CategoryFeature> ConvertFeatureToBl(FeatureDAL feature, string lng);

    // Generate* methods always read from the database (used to (re)build the Redis caches).
    Task<Menu?> GenerateMenu(int menuId, string lng);
    Task<MenuCategoryDAL?> GenerateMenuCategoryDAL(int categoryId);
    Task<List<MenuCategoryDAL>> GenerateAllCategoriesDal();
    Task<List<int>?> GenerateAllCategoryIds(int categoryId);

    Task<IEnumerable<Product?>?> GetRecommendation();
    Task<IEnumerable<Product>?> GetRecommendationByCategory(int categoryId);
}
