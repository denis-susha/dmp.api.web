using DMP.BL.Models;
using DMP.BL.Models.Sales;
using DMP.BL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[Authorize(Policy = "RequireIsSeller"), ApiController]
[Route("[controller]")]
public class SalesController(ILogger<SalesController> logger, ISalesService salesService) : ControllerCustom(logger)
{
    [HttpPost("list")]
    public async Task<ActionResult<GetSalesResponse>> GetSales([FromBody] TableQuery request)
    {
        try
        {
            var result = await salesService.GetSalesList(UserId, request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get sales list");
            return StatusCode(500);
        }
    }

    [HttpGet]
    public async Task<ActionResult<SalesViewHeader>> GetSale([FromQuery] int salesHeaderId)
    {
        try
        {
            var result = await salesService.GetSaleView(UserId, salesHeaderId);

            if (result is null)
            {
                return NotFound();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get sale {SalesHeaderId}", salesHeaderId);
            return StatusCode(500);
        }
    }
}
