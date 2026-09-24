using DMP.BL.Models.Auth;
using DMP.BL.Models.User;
using DMP.BL.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController(ILogger<UserController> logger, IUserService userService) : ControllerCustom(logger)
{
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<UserInfo>> GetUser()
    {
        try
        {
            var userInfo = await userService.GetUser(UserId);

            if (userInfo is null)
            {
                return BadRequest();
            }

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get user");
            return StatusCode(500);
        }
    }

    [Authorize]
    [HttpGet("dynamic-info/client")]
    public async Task<ActionResult<ClientDynamicInfo>> GetDynamicInfoClient()
    {
        try
        {
            var result = await userService.GetClientDynamicInfo(UserId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get client dynamic info");
            return StatusCode(500);
        }
    }

    [Authorize(Policy = "RequireIsSeller")]
    [HttpGet("dynamic-info/seller")]
    public async Task<ActionResult<UserDynamicInfo>> GetDynamicInfoSeller()
    {
        try
        {
            var result = await userService.GetUserDynamicInfo(UserId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get seller dynamic info");
            return StatusCode(500);
        }
    }

    [Authorize(Policy = "RequireIsSeller")]
    [HttpGet("welcome-info/seller")]
    public async Task<ActionResult<UserWelcomeInfo>> GetWelcomeInfoSeller()
    {
        try
        {
            var result = await userService.GetWelcomeInfoSeller(UserId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get seller welcome info");
            return StatusCode(500);
        }
    }

    [Authorize(Policy = "RequireIsSeller")]
    [HttpPost("complete-tutorial/seller")]
    public async Task<ActionResult<bool>> CompleteTutorial()
    {
        try
        {
            var result = await userService.CompleteTutorial(UserId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to complete seller tutorial");
            return StatusCode(500);
        }
    }

    [Authorize]
    [HttpPost("become-seller")]
    public async Task<ActionResult<bool>> BecomeSeller()
    {
        try
        {
            var result = await userService.BecomeSeller(UserId);

            if (result)
            {
                DeleteAuthCookies();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to become seller");
            return StatusCode(500);
        }
    }

    [Authorize]
    [HttpGet("settings")]
    public async Task<ActionResult<UserSettings>> GetUserSettings()
    {
        try
        {
            var result = await userService.GetUserSettings(UserId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get user settings");
            return StatusCode(500);
        }
    }

    [Authorize]
    [HttpPost("profile-settings")]
    public async Task<ActionResult<bool>> UpdateUserProfileSettings([FromBody] UserProfileSettings request)
    {
        try
        {
            var validationResult = request.Validate();

            if (!validationResult.IsValid)
            {
                Logger.LogError("Invalid request: {Request}", request);
                return BadRequest();
            }

            var result = await userService.UpdateUserProfileSettings(UserId, request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update user profile settings");
            return StatusCode(500);
        }
    }

    [Authorize]
    [HttpGet("delete")]
    public async Task<ActionResult<bool>> DeleteAccount()
    {
        try
        {
            var refreshToken = HttpContext.Request.Cookies["refresh_token"]!;

            await userService.DeleteAccount(UserId, refreshToken);

            DeleteAuthCookies();

            return Ok();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete account");
            return StatusCode(500);
        }
    }
}
