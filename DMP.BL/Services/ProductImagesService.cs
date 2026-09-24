using DMP.BL.Models.ProductCreation;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using SixLabors.ImageSharp;
using IMinioClientFactory = DMP.BL.Factories.IMinioClientFactory;

namespace DMP.BL.Services;

public class ProductImagesService(ILogger<ProductImagesService> logger, IMinioClientFactory minioClientFactory)
    : IProductImagesService
{
    private const string ProductImagesBucketName = "images";

    public async Task<string> SaveImage(byte[] imageBytes, string qualityFolder, int imageIndex, int productId, string extension)
    {
        var path = $"product/{productId}/{qualityFolder}/{imageIndex}{extension}";

        try
        {
            return await UploadImage(imageBytes, path) ? $"{ProductImagesBucketName}/{path}" : string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save image {Path}", path);
            throw;
        }
    }

    public Task<(CreateOrUpdateProductStatus status, NewProductFile? savedImage)> SaveProductImage(
        NewProductFile newProductFile, int productId) =>
        SaveOriginalImage(newProductFile, $"product/{productId}/original/");

    public Task<(CreateOrUpdateProductStatus status, NewProductFile? savedImage)> SaveStoreImage(
        NewProductFile newProductFile, Guid storeId) =>
        SaveOriginalImage(newProductFile, $"store/{storeId}/original/");

    private async Task<(CreateOrUpdateProductStatus status, NewProductFile? savedImage)> SaveOriginalImage(
        NewProductFile newProductFile, string folder)
    {
        NewProductFile imageToSave;

        try
        {
            imageToSave = await PrepareImage(newProductFile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to convert uploaded image {FileName}", newProductFile.FileName);

            return (CreateOrUpdateProductStatus.InvalidImage, null);
        }

        var path = folder + imageToSave.FileName;

        try
        {
            if (!await UploadImage(imageToSave.Data, path))
            {
                logger.LogError("Image upload to S3 returned no ETag for {Path}", path);

                return (CreateOrUpdateProductStatus.InternalError, null);
            }

            imageToSave.StoredPath = $"{ProductImagesBucketName}/{path}";

            return (CreateOrUpdateProductStatus.Success, imageToSave);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload image {Path} to S3", path);

            return (CreateOrUpdateProductStatus.InternalError, null);
        }
    }

    /// <returns><c>true</c> when S3 confirmed the upload with an ETag.</returns>
    private async Task<bool> UploadImage(byte[] data, string path)
    {
        var minioClient = minioClientFactory.CreateClient().Build();

        await using var stream = new MemoryStream(data);

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(ProductImagesBucketName)
            .WithObject(path)
            .WithObjectSize(stream.Length)
            .WithStreamData(stream)
            .WithContentType("application/octet-stream");

        var response = await minioClient.PutObjectAsync(putObjectArgs).ConfigureAwait(false);

        return !string.IsNullOrEmpty(response?.Etag);
    }

    private static async Task<NewProductFile> PrepareImage(NewProductFile newProductFile)
    {
        try
        {
            return new NewProductFile
            {
                Extension = ".webp",
                Data = await ConvertToWebp(newProductFile.Data),
                Quality = "wc1000",
                FileName = Ulid.NewUlid() + ".webp"
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Invalid image.", ex);
        }
    }

    private static async Task<byte[]> ConvertToWebp(byte[] data)
    {
        using var inStream = new MemoryStream(data);
        using var image = await Image.LoadAsync(inStream);

        using var outStream = new MemoryStream();
        await image.SaveAsWebpAsync(outStream);

        return outStream.ToArray();
    }
}
