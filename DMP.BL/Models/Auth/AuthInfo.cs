namespace DMP.BL.Models.Auth;

public class AuthInfo
{
    public UserInfo UserInfo { get; set; } = null!;
    public string Expiry { get; set; } = null!;
}
