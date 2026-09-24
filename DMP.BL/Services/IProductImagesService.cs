using DMP.BL.Models.ProductCreation;

namespace DMP.BL.Services;

public interface IProductImagesService
{
    Task<string> SaveImage(byte[] imageBytes, string qualityFolder, int imageIndex, int productId, string extension);

    Task<(CreateOrUpdateProductStatus status, NewProductFile? savedImage)> SaveProductImage(
        NewProductFile newProductFile, int productId);

    Task<(CreateOrUpdateProductStatus status, NewProductFile? savedImage)> SaveStoreImage(
        NewProductFile newProductFile, Guid storeId);
}
