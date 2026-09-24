using API.Web.Attributes;
using DMP.BL.Models.Registration;
using DMP.BL.RequestModels;
using DMP.BL.Services.User;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class RegistrationController(ILogger<RegistrationController> logger, IRegistrationService registrationService)
    : ControllerCustom(logger)
{
    [Route("seller")]
    [HttpPost]
    [ExtractLocale]
    public async Task<IActionResult> RegisterSeller([FromBody] RegistrationRequest request,
        [HideFromOpenApi] string locale = "")
    {
        try
        {
            var validationResult = Validate(request);

            if (!validationResult.IsValid)
            {
                return BadRequest();
            }

            var result = await registrationService.RegisterSeller(request, locale, HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to register seller");
            return StatusCode(500);
        }
    }

    [Route("client")]
    [HttpPost]
    [ExtractLocale]
    public async Task<IActionResult> RegisterClient([FromBody] RegistrationRequest request,
        [HideFromOpenApi] string locale = "")
    {
        try
        {
            var validationResult = Validate(request);

            if (!validationResult.IsValid)
            {
                return BadRequest();
            }

            var result = await registrationService.RegisterClient(request, locale, HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to register client");
            return StatusCode(500);
        }
    }

    [Route("confirm-email")]
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest();
            }

            var result = await registrationService.ConfirmEmail(token);
            if (result.Response.Status == ConfirmEmailStatus.Success)
            {
                SetAuthCookies(result);
            }

            return Ok(result.Response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to confirm email");
            return StatusCode(500);
        }
    }

    [HttpPost("resend-confirmation")]
    [ExtractLocale]
    public async Task<ActionResult<ResendEmailConfirmationStatus>> ResendEmailConfirmation(
        [FromBody] ResendEmailConfirmationRequest request, [HideFromOpenApi] string locale = "")
    {
        try
        {
            var validationResult = Validate(request);

            if (!validationResult.IsValid)
            {
                return BadRequest();
            }

            var result = await registrationService.ResendEmailConfirmation(request, locale);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to resend email confirmation");
            return StatusCode(500);
        }
    }

    [HttpPost("forgot-password")]
    [ExtractLocale]
    public async Task<ActionResult<SendForgotPasswordStatus>> SendForgotPassword(
        [FromBody] ResendEmailConfirmationRequest request, [HideFromOpenApi] string locale = "")
    {
        try
        {
            var validationResult = Validate(request);

            if (!validationResult.IsValid)
            {
                return BadRequest();
            }

            var result = await registrationService.SendForgotPassword(request, locale);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send forgot-password email");
            return StatusCode(500);
        }
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ConfirmEmailStatus>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            var validationResult = Validate(request);

            if (!validationResult.IsValid)
            {
                return BadRequest();
            }

            var result = await registrationService.ResetPassword(request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to reset password");
            return StatusCode(500);
        }
    }
}
