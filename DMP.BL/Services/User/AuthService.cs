using System.Security.Claims;
using DMP.BL.Constants;
using DMP.BL.Helpers;
using DMP.BL.Models.Auth;
using DMP.BL.Models.Enumerations;
using DMP.DataAccess;
using DMP.DataAccess.Models;
using DMP.DataAccess.Models.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DMP.BL.Services.User;

public class AuthService(
    ILogger<AuthService> logger,
    IDbContextFactory<DmpDbContext> dmpContextFactory,
    IJwtTokenService jwtTokenService,
    IRedisService redisService,
    IOptions<JwtSettings> jwtSettings,
    IUserService userService) : IAuthService
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task<UserLoginResult> Login(UserLoginRequest request)
    {
        var result = new UserLoginResult();

        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var email = EmailHelper.Normalize(request.Email);
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user is null)
        {
            logger.LogInformation("User with email {Email} doesn't exist in db.", email);
            result.Status = LoginStatus.Unauthorized;
            return result;
        }

        if (user.Status != UserStatus.Active)
        {
            logger.LogInformation("User with email {Email} is not active.", email);
            result.Status = LoginStatus.Unauthorized;
            return result;
        }

        if (!PasswordHelper.VerifyPassword(request.Password, user.Password, user.Salt))
        {
            logger.LogInformation("Wrong password for email {Email}.", email);
            result.Status = LoginStatus.Unauthorized;
            return result;
        }

        var userAuthTokens = await GenerateTokens(user);

        result.AccessToken = userAuthTokens.AccessToken;
        result.RefreshToken = userAuthTokens.RefreshToken;
        result.AuthInfo = new AuthInfo
        {
            Expiry = userAuthTokens.RefreshTokenExpiry,
            UserInfo = userService.GetUserInfo(user),
        };

        return result;
    }

    public async Task<UserLoginResult> RefreshToken(string accessToken, string oldRefreshToken)
    {
        var result = new UserLoginResult();

        var decodedToken = jwtTokenService.DecodeToken(accessToken);
        var userIdStr = decodedToken.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            logger.LogInformation("Wrong access token.");
            result.Status = LoginStatus.Unauthorized;
            return result;
        }

        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user is null || user.Status != UserStatus.Active)
        {
            logger.LogInformation("Wrong userId {UserId}.", userId);
            result.Status = LoginStatus.Unauthorized;
            return result;
        }

        var cacheKey = string.Format(CacheKeys.UserRefreshToken, user.UserId, oldRefreshToken);
        var storedRefreshToken = await redisService.GetCacheValue<string>(cacheKey);

        if (storedRefreshToken is null)
        {
            logger.LogInformation("Invalid or expired refresh token for user {UserId}.", userId);
            result.Status = LoginStatus.Unauthorized;
            return result;
        }

        result.AccessToken = GenerateAccessToken(user);
        result.RefreshToken = oldRefreshToken;
        result.AuthInfo = new AuthInfo
        {
            UserInfo = userService.GetUserInfo(user),
        };

        return result;
    }

    public async Task<UserAuthTokens> GenerateTokens(UserDAL user)
    {
        var refreshToken = jwtTokenService.GenerateRefreshToken();

        var cacheKey = string.Format(CacheKeys.UserRefreshToken, user.UserId, refreshToken);
        await redisService.SetCacheValue(cacheKey, refreshToken, TimeSpan.FromDays(_jwtSettings.RefreshTokenExpirationDays));

        return new UserAuthTokens
        {
            AccessToken = GenerateAccessToken(user),
            RefreshToken = refreshToken,
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays).ToString("o")
        };
    }

    public async Task Logout(Guid userId, string refreshToken) =>
        await redisService.DeleteCacheValue(string.Format(CacheKeys.UserRefreshToken, userId, refreshToken));

    private string GenerateAccessToken(UserDAL user) =>
        jwtTokenService.GenerateAccessToken(GenerateClaims(user), _jwtSettings);

    private static List<Claim> GenerateClaims(UserDAL user) =>
    [
        new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new("isSeller", user.Flags.HasFlag(UserFlags.IsSeller).ToString().ToLower()),
        new(ClaimTypes.Role, user.Flags.HasFlag(UserFlags.IsAdmin) ? "admin" : user.Flags.HasFlag(UserFlags.IsSeller) ? "seller" : "client")
    ];
}
