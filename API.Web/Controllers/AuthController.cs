using DMP.BL.Models.Auth;
using DMP.BL.Models.Enumerations;
using DMP.BL.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(ILogger<AuthController> logger, IAuthService authService) : ControllerCustom(logger)
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
    {
        try
        {
            var validationResult = request.Validate();

            if (!validationResult.IsValid)
            {
                return BadRequest();
            }

            var result = await authService.Login(request);

            if (result.Status == LoginStatus.Unauthorized)
            {
                return Unauthorized();
            }

            SetAuthCookies(result);

            return Ok(result.AuthInfo);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Login failed");
            return StatusCode(500);
        }
    }

    [HttpGet("refresh")]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies["refresh_token"];
        var accessToken = Request.Cookies["access_token"];

        if (string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(accessToken))
        {
            Logger.LogInformation("Invalid request on refresh.");
            return BadRequest();
        }

        var result = await authService.RefreshToken(accessToken, refreshToken);

        if (result.Status == LoginStatus.Unauthorized)
        {
            DeleteAuthCookies();
            return Unauthorized();
        }

        SetAuthCookies(result);

        return Ok(result.AuthInfo);
    }

    [Authorize]
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = HttpContext.Request.Cookies["refresh_token"];

        if (!string.IsNullOrEmpty(refreshToken))
        {
            await authService.Logout(UserId, refreshToken);
        }

        DeleteAuthCookies();

        return Ok();
    }
}
