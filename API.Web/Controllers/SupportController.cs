using DMP.BL.Models.Support;
using DMP.BL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[Authorize, ApiController]
[Route("[controller]")]
public class SupportController(ILogger<SupportController> logger, ISupportService supportService) : ControllerCustom(logger)
{
    [Authorize(Policy = "RequireIsSeller")]
    [HttpPost("seller/create")]
    public async Task<ActionResult<bool>> CreateSellerTicket([FromBody] CreateTicketRequest request)
    {
        try
        {
            var validationResult = request.Validate();

            if (!validationResult.IsValid)
            {
                Logger.LogError("Invalid CreateTicketRequest: {Request}. Error: {ErrorMsg}", request,
                    validationResult.ErrorMsg);

                return BadRequest();
            }

            var result = await supportService.CreateTicket(UserId, request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create seller support ticket");
            return StatusCode(500);
        }
    }

    [HttpGet("tickets")]
    public async Task<ActionResult<GetSupportTicketsResponse>> GetTickets([FromQuery] int page, [FromQuery] bool archived)
    {
        try
        {
            if (page < 0)
            {
                Logger.LogError("Invalid params.");

                return BadRequest();
            }

            var result = await supportService.GetTickets(UserId, archived, page);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get support tickets");
            return StatusCode(500);
        }
    }

    [HttpGet("ticket")]
    public async Task<ActionResult<GetSupportTicketsResponse>> GetTicket([FromQuery] int supportTicketId)
    {
        try
        {
            if (supportTicketId < 1)
            {
                Logger.LogError("Invalid params.");

                return BadRequest();
            }

            var result = await supportService.GetTicket(UserId, supportTicketId);

            if (result is null)
            {
                return BadRequest();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get support ticket {SupportTicketId}", supportTicketId);
            return StatusCode(500);
        }
    }

    [HttpPost("message")]
    public async Task<ActionResult<bool>> SendMessage([FromBody] AddTicketMessageRequest request)
    {
        try
        {
            var validationResult = request.Validate();

            if (!validationResult.IsValid)
            {
                Logger.LogError("Invalid AddTicketMessageRequest: {Request}. Error: {ErrorMsg}", request,
                    validationResult.ErrorMsg);

                return BadRequest();
            }

            var result = await supportService.AddMessage(UserId, request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to add support ticket message");
            return StatusCode(500);
        }
    }
}
