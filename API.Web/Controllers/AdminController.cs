using DMP.BL.Models.Admin;
using DMP.BL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[Authorize(Policy = "RequireAdmin"), ApiController]
[Route("[controller]")]
public class AdminController(ILogger<AdminController> logger, IAdminService adminService) : ControllerCustom(logger)
{
    [HttpPost("producttowork")]
    public async Task<ActionResult<NewProductAdmin>> ProductToWork([FromQuery] int productId)
    {
        try
        {
            var result = await adminService.ProductToWork(UserId, productId);

            if (result is null)
            {
                return NotFound();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to take product {ProductId} to work", productId);
            return StatusCode(500);
        }
    }

    [HttpPost("finishmoderation")]
    public async Task<ActionResult<NewProductAdmin>> FinishModeration([FromBody] FinishModerationRequest request)
    {
        try
        {
            if (!request.Validate().IsValid)
            {
                return BadRequest();
            }

            Logger.LogInformation("Admin {AdminId} sent finish moderation for product {ProductId} with status {Status}",
                UserId, request.ProductId, request.Status);

            var result = await adminService.FinishModeration(request);

            if (!result)
            {
                return BadRequest();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to finish moderation for product {ProductId}", request.ProductId);
            return StatusCode(500);
        }
    }

    [HttpPost("create-payout")]
    public async Task<IActionResult> CreatePayout([FromBody] CreatePayoutRequest request)
    {
        try
        {
            var validationResult = Validate(request);

            if (!validationResult.IsValid)
            {
                Logger.LogError("Request validation error: {ErrorMsg}", validationResult.ErrorMsg);

                return BadRequest();
            }

            Logger.LogInformation("Admin {AdminId} sent payout {PayoutId}, amount {Amount}",
                UserId, request.PayoutId, request.Amount);

            await adminService.CreatePayout(request, UserId);

            return Ok();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create payout {PayoutId}", request.PayoutId);
            return StatusCode(500);
        }
    }

    [HttpPost("create-incoming-transfer")]
    public async Task<IActionResult> CreateIncomingTransfer([FromBody] CreateIncomingTransferRequest request)
    {
        try
        {
            var validationResult = Validate(request);

            if (!validationResult.IsValid)
            {
                Logger.LogError("Request validation error: {ErrorMsg}", validationResult.ErrorMsg);

                return BadRequest();
            }

            Logger.LogInformation("Admin {AdminId} sent incoming transfer for invoice {InvoiceId}",
                UserId, request.InvoiceId);

            await adminService.CreateIncomingTransfer(request, UserId);

            return Ok();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create incoming transfer for invoice {InvoiceId}", request.InvoiceId);
            return StatusCode(500);
        }
    }

    [HttpPost("create-bonus")]
    public async Task<IActionResult> CreateBonus([FromBody] CreateBonusRequest request)
    {
        try
        {
            var validationResult = Validate(request);

            if (!validationResult.IsValid)
            {
                Logger.LogError("Request validation error: {ErrorMsg}", validationResult.ErrorMsg);

                return BadRequest();
            }

            Logger.LogInformation("Admin {AdminId} sent bonus for target user {TargetUserId}",
                UserId, request.TargetUserId);

            await adminService.CreateBonus(request, UserId);

            return Ok();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create bonus for target user {TargetUserId}", request.TargetUserId);
            return StatusCode(500);
        }
    }
}
