using DMP.BL.Models;
using DMP.BL.Models.Enumerations;
using DMP.BL.Models.Search;

namespace DMP.BL.Services;

public interface IRedisService
{
    void EnsureProductCatIndex(string catIdx);
    void EnsureProductIndex();
    void EnsureProductSearchIndex();

    Task<T?> GetCacheValue<T>(string key);
    Task<bool> SetCacheValue<T>(string key, T value, TimeSpan? expiry = null);
    Task<bool> DeleteCacheValue(string key);

    Task<int[]?> GetProductIds(string category, int page, short pageSize, ProductSorting sorting);

    bool RemoveProduct(int productId, string rootCategoryId);
    bool SetProducts(List<Product> products, string rootCategoryId);

    Task<(IEnumerable<Product>? products, int totalCount)> SearchProducts(ProductSearchParams searchParams);
    Task<(Product? product, string? rootId)> SearchProduct(int productId);
    Task<IEnumerable<Product?>> SearchProducts(int[] ids);

    Task<FullTextSearchProductsResponse> FullTextSearch(string query, string lng);
    Task<(List<Product>? products, long totalCount)> FullTextSearchProducts(string query, int offset, int pageSize);
    bool SetSearchCategories(Dictionary<(int, string), SearchCategory> searchCategories);
    Task AddSysNotification(string message);
}
