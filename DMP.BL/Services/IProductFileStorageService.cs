namespace DMP.BL.Services;

public interface IProductFileStorageService
{
    Task<string> GetFileUploadPresignedUrl(int expiry, int productId, string fileName);
    Task<string?> GetFileDownloadPresignedUrl(string path, int expiry);
}
