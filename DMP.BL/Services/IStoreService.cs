using DMP.BL.Models.ProductCreation;
using DMP.BL.Models.Store;

namespace DMP.BL.Services;

public interface IStoreService
{
    Task<Store?> GetStore(Guid userId);
    Task<bool> UpdateStore(Guid userId, Store store);
    Task<UploadProductImageResponse> UploadStoreImage(Guid userId, NewProductFile imageFile);
}
