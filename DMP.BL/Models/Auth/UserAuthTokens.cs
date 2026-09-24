namespace DMP.BL.Models.Auth;

public class UserAuthTokens
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public string RefreshTokenExpiry { get; set; } = null!;
}
