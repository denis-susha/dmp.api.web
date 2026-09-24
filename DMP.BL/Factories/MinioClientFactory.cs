using Minio;

namespace DMP.BL.Factories;

public class MinioClientFactory(string endpoint, string accessKey, string secretKey) : IMinioClientFactory
{
    public IMinioClient CreateClient() => CreateClient(endpoint, accessKey, secretKey);

    public IMinioClient CreateClient(string endpoint, string accessKey, string secretKey) =>
        new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey);
}
