using DMP.BL.Models.Enumerations;

namespace DMP.BL.Models.Auth;

public class UserLoginResult : UserAuthTokens
{
    public LoginStatus Status { get; set; }
    public AuthInfo AuthInfo { get; set; } = null!;
}
