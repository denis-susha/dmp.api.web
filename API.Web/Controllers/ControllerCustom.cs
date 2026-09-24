using System.Security.Claims;
using DMP.BL.Models.Auth;
using DMP.BL.ModelValidation;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

public abstract class ControllerCustom(ILogger logger) : ControllerBase
{
    private const string AccessTokenCookie = "access_token";
    private const string RefreshTokenCookie = "refresh_token";

    protected ILogger Logger { get; } = logger;

    public Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [NonAction]
    public ValidationResult Validate<T>(T model) where T : IValidate
    {
        var result = model.Validate();

        if (!result.IsValid)
        {
            Logger.LogWarning("Incoming model is invalid. Error: {ErrorMsg}.", result.ErrorMsg);
        }

        return result;
    }

    [NonAction]
    public void SetAuthCookies(UserAuthTokens response)
    {
        var cookieOptions = CreateAuthCookieOptions();

        Response.Cookies.Append(AccessTokenCookie, response.AccessToken, cookieOptions);
        Response.Cookies.Append(RefreshTokenCookie, response.RefreshToken, cookieOptions);
    }

    [NonAction]
    public void DeleteAuthCookies()
    {
        var cookieOptions = CreateAuthCookieOptions();

        Response.Cookies.Delete(AccessTokenCookie, cookieOptions);
        Response.Cookies.Delete(RefreshTokenCookie, cookieOptions);
    }

    private CookieOptions CreateAuthCookieOptions()
    {
        var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = env.IsProduction(),
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddYears(1),
            // Local development runs on localhost, so the cookie must not be bound to the production domain.
            Domain = env.IsEnvironment("Local") ? "" : ".filezon.com"
        };
    }
}
