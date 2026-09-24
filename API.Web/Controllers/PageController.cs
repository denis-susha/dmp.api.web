using API.Web.Attributes;
using DMP.BL.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class PageController(ILogger<PageController> logger, IPageService pageService) : ControllerCustom(logger)
{
    [HttpGet("json")]
    [ExtractLocale]
    public async Task<ActionResult<string>> GetJson([FromQuery] string path, [HideFromOpenApi] string locale = "")
    {
        try
        {
            var result = await pageService.GetJson(path, locale);

            if (result is null)
            {
                return BadRequest();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get page JSON for {Path}", path);
            return StatusCode(500);
        }
    }
}
