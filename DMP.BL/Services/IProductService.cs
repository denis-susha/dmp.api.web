using DMP.BL.Models;
using DMP.BL.Models.Search;
using DMP.BL.Models.Store;
using DMP.DataAccess.Models;

namespace DMP.BL.Services;

public interface IProductService
{
    Task<ProductData?> GetProductData(string lng, int productId, string productSlug);
    Task<IEnumerable<Product?>> GetProducts(int[] ids);
    Task<Product?> GetProduct(int id);
    Task UpdateCacheProducts(List<int> productIds);
    Task<FullTextSearchProductsResponse> SearchProducts(string query, string lng);
    Task<FullTextSearchResponse> FullTextSearchProducts(string query, PaginationModel pagination, string lng);
    Task RemoveProductsFromCache(List<ProductDAL> products);
    Task<List<Product?>> GetFavoritesProducts(int[] productIds);
    Task<ProductStoreInfo?> GetProductStoreInfo(int productId);
    Task<string> GetProductFeaturesValues(Product product, string lng);
}
