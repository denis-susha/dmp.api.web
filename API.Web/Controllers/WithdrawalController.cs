using DMP.BL.Models;
using DMP.BL.Models.Withdrawal;
using DMP.BL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[Authorize(Policy = "RequireIsSeller"), ApiController]
[Route("[controller]")]
public class WithdrawalController(ILogger<WithdrawalController> logger, IWithdrawalService withdrawalService)
    : ControllerCustom(logger)
{
    [HttpPost("list")]
    public async Task<ActionResult<GetPayoutsResponse>> GetPayouts([FromBody] TableQuery request)
    {
        try
        {
            var result = await withdrawalService.GetPayoutsList(UserId, request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get payouts list");
            return StatusCode(500);
        }
    }
}
