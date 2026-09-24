namespace API.Web;

public static class Constants
{
    public const string CategorySlugPattern = @"(.*?)-(\d+)$";
    public const string ProductSlugPattern = @"(.*?)-(\d+)$";

    public static readonly string[] AllowedProductImagesMimeTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
    public static readonly string[] AllowedProductImagesExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
}
