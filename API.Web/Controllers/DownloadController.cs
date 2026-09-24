using DMP.BL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[Authorize, ApiController]
[Route("[controller]")]
public class DownloadController(ILogger<DownloadController> logger, IDownloadOrderService downloadOrderService)
    : ControllerCustom(logger)
{
    [HttpGet]
    public async Task<IActionResult> Download([FromQuery] int orderId, int fileLineIdentificator)
    {
        try
        {
            var result = await downloadOrderService.DownloadProduct(UserId, orderId, fileLineIdentificator);

            if (result is null)
            {
                return BadRequest();
            }

            return new RedirectResult(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to download file {FileLineIdentificator} of order {OrderId}",
                fileLineIdentificator, orderId);
            return StatusCode(500);
        }
    }
}
