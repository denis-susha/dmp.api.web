using Minio;

namespace DMP.BL.Factories;

public interface IMinioClientFactory
{
    /// <summary>Creates a client configured with the default (admin) credentials.</summary>
    IMinioClient CreateClient();

    IMinioClient CreateClient(string endpoint, string accessKey, string secretKey);
}
