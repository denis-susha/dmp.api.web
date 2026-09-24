using DMP.BL.Models.Dashboard;
using DMP.BL.Models.Enumerations;
using DMP.BL.Models.Seller;
using DMP.BL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[Authorize(Policy = "RequireIsSeller"), ApiController]
[Route("[controller]")]
public class SellerController(ILogger<SellerController> logger, ISellerService sellerService) : ControllerCustom(logger)
{
    [HttpGet]
    public async Task<ActionResult<GetSellerUserResponse>> GetUser()
    {
        try
        {
            var result = await sellerService.GetSellerUser(UserId);

            if (result.UserInfo is null)
            {
                return BadRequest();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get seller user");
            return StatusCode(500);
        }
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardInfo>> GetDashboardInfo([FromQuery] Period period)
    {
        try
        {
            var result = await sellerService.GetDashboardInfo(UserId, period);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get dashboard info for period {Period}", period);
            return StatusCode(500);
        }
    }
}
