using System.Text.Json;
using API.Web.Attributes;
using API.Web.Hubs;
using API.Web.Models.WsMessages;
using DMP.BL.Constants;
using DMP.BL.Models.WorkerNotification;
using DMP.BL.Services.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.Web.Controllers;

/// <summary>
/// Receives notifications from the background worker (Basic auth) and forwards them to connected SignalR clients.
/// </summary>
[BasicAuthorize]
[Route("messages/[controller]")]
public class WorkerNotificationController(
    ILogger<WorkerNotificationController> logger,
    IHubContext<PaymentHub> hubContext,
    UserConnectionService connectionService) : ControllerCustom(logger)
{
    [HttpPost("paymentupdate")]
    public async Task<IActionResult> PaymentUpdate([FromBody] PaymentUpdateRequest request)
    {
        try
        {
            var connectionIds = connectionService.GetConnectionIds(request.UserId);
            if (connectionIds is not null)
            {
                var message = new WsPaymentUpdateMessage
                {
                    PaymentStatus = request.Status,
                    SentAmount = request.SentAmount,
                    OrderId = request.OrderId,
                };

                var jsonMsg = JsonSerializer.Serialize(message, BlConstants.JsonSerializerOptions);

                foreach (var connectionId in connectionIds)
                {
                    await hubContext.Clients.Client(connectionId).SendAsync(PaymentHub.ReceiveMessageMethod, jsonMsg);
                }

                Logger.LogInformation("WS message sent to {UserId} with status {Status}.", request.UserId, request.Status);

                return Ok("Message sent.");
            }

            Logger.LogInformation("WS message wasn't sent to {UserId} with status {Status}: user not connected.",
                request.UserId, request.Status);

            return Ok("User not connected.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to forward payment update for order {OrderId}", request.OrderId);
            return StatusCode(500);
        }
    }
}
