using DMP.BL.Models;
using DMP.BL.Models.ProductCreation;

namespace DMP.BL.Services;

public interface IProductManagerService
{
    Task<GetNewProductResponse> GetProduct(Guid userId, int productId);
    Task<ProductView?> GetProductView(Guid userId, int productId);
    Task<IEnumerable<ProductCategory>> GetProductCategories(string lng, int menuId);
    Task<IEnumerable<ProductCategory>> GenerateProductCategories(string lng, int menuId);
    Task<IEnumerable<CategoryFeature>> GetCategoryFeatures(int categoryId, string lng);
    Task<List<CategoryFeature>> GenerateCategoryFeatures(int categoryId, string lng);

    Task<CreateOrUpdateProductResponse> CreateOrUpdateNewProduct(Guid userId, NewProductRequest newProduct, string lng);

    Task<UploadProductImageResponse> UploadProductImage(Guid userId, NewProductFile imageFile, int productId);
    Task<bool> SetProductImages(Guid userId, NewProductImage[]? newProductImages, int productId);
    Task<GenerateUploadUrlResponse> GenerateUploadUrl(Guid userId, GenerateUploadUrlRequest request);
    Task<GetProductDataItemsResponse> GetProductDataItems(Guid userId, int productId);
    Task<bool> DeleteProductFile(Guid userId, SetProductFileUploadedRequest request);
    Task<bool> SetProductFileUploaded(Guid userId, SetProductFileUploadedRequest request);
    Task<bool> SetProductFiles(Guid userId, SetProductFilesRequest request);
    Task<bool> FinishProductCreation(Guid userId, int productId);
    Task<GetProductListResponse> GetProductList(Guid userId, GetProductListRequest request);
    Task<bool> ChangeProductStatus(Guid userId, ChangeProductStatusRequest request);
}
