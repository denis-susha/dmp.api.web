using System.Text.RegularExpressions;
using DMP.BL.Models.AppSettings;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using IMinioClientFactory = DMP.BL.Factories.IMinioClientFactory;

namespace DMP.BL.Services;

public partial class ProductFileStorageService(
    ILogger<ProductFileStorageService> logger,
    IMinioClientFactory minioClientFactory,
    IApplicationSettingsService applicationSettingsService) : IProductFileStorageService
{
    private const string StorageBucketName = "storage";
    private const string ProductUploadDataBucketName = "dataupload";

    private readonly MinioSettings _minioSettings = applicationSettingsService.MinioSettings;

    public async Task<string> GetFileUploadPresignedUrl(int expiry, int productId, string fileName)
    {
        var presignedPutObjectArgs = new PresignedPutObjectArgs()
            .WithBucket(ProductUploadDataBucketName)
            .WithObject($"{productId}/{fileName}")
            .WithExpiry(expiry);

        var minioClient = minioClientFactory.CreateClient(_minioSettings.Endpoint,
            _minioSettings.SellerSettings.AccessKey, _minioSettings.SellerSettings.SecretKey).Build();
        var uploadUrl = await minioClient.PresignedPutObjectAsync(presignedPutObjectArgs);

        return ToPublicUrl(uploadUrl);
    }

    public async Task<string?> GetFileDownloadPresignedUrl(string path, int expiry)
    {
        try
        {
            var minioClient = minioClientFactory.CreateClient(_minioSettings.Endpoint,
                _minioSettings.ClientSettings.AccessKey, _minioSettings.ClientSettings.SecretKey).Build();

            var args = new PresignedGetObjectArgs()
                .WithBucket(StorageBucketName)
                .WithObject(path)
                .WithExpiry(expiry);

            var url = await minioClient.PresignedGetObjectAsync(args);

            return ToPublicUrl(url);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Product data presigned URL generation failed for {Path}", path);

            return null;
        }
    }

    // Presigned URLs are generated against the internal (container) endpoint; swap in the public S3 host.
    private string ToPublicUrl(string url) => SchemeAndHostRegex().Replace(url, _minioSettings.S3PublicEndpoint, 1);

    [GeneratedRegex(@"^https?:\/\/[^\/]+")]
    private static partial Regex SchemeAndHostRegex();
}
