namespace DMP.BL.Models.AppSettings;

public class MinioUserSettings
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}

public class MinioSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string S3PublicEndpoint { get; set; } = string.Empty;

    public MinioUserSettings SellerSettings { get; set; } = new();
    public MinioUserSettings ClientSettings { get; set; } = new();
}
