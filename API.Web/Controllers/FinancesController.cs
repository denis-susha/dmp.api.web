using DMP.BL.Models.Finances;
using DMP.BL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[Authorize(Policy = "RequireIsSeller"), ApiController]
[Route("[controller]")]
public class FinancesController(ILogger<FinancesController> logger, IFinancesService financesService)
    : ControllerCustom(logger)
{
    [HttpGet("accounts")]
    public async Task<ActionResult<List<AccountBalance>>> GetAccountsBalances()
    {
        try
        {
            var result = await financesService.GetAccountsBalances(UserId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get account balances");
            return StatusCode(500);
        }
    }

    [HttpGet]
    public async Task<ActionResult<GetFinancesResponse>> GetFinances()
    {
        try
        {
            var result = await financesService.GetFinances(UserId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get finances");
            return StatusCode(500);
        }
    }
}
