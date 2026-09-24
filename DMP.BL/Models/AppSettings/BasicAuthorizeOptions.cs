namespace DMP.BL.Models.AppSettings;

public class BasicAuthorizeOptions
{
    public const string Position = "BasicAuthorize";

    public string Password { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
}
