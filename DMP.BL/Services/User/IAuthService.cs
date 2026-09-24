using DMP.BL.Models.Auth;
using DMP.DataAccess.Models;

namespace DMP.BL.Services.User;

public interface IAuthService
{
    Task<UserLoginResult> Login(UserLoginRequest request);
    Task<UserLoginResult> RefreshToken(string accessToken, string oldRefreshToken);
    Task<UserAuthTokens> GenerateTokens(UserDAL user);
    Task Logout(Guid userId, string refreshToken);
}
