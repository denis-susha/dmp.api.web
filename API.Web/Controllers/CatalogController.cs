using System.Text.RegularExpressions;
using API.Web.Attributes;
using API.Web.Helpers;
using API.Web.Models;
using DMP.BL.Models;
using DMP.BL.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class CatalogController(ILogger<CatalogController> logger, IMenuService menuService) : ControllerBase
{
    [HttpGet]
    [Route("menu")]
    [ExtractLocale]
    public async Task<ActionResult<Menu>> GetMenu(
        [HideFromOpenApi] string locale, [FromQuery] int menuId = 1)
    {
        try
        {
            var menu = await menuService.GetMenu(locale, menuId);

            if (menu is null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource not found",
                    Detail = $"Resource with ID {menuId} was not found."
                });
            }

            return Ok(menu);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get menu {MenuId}", menuId);
            return StatusCode(500);
        }
    }

    [HttpGet]
    [Route("category")]
    [ExtractLocale]
    public async Task<ActionResult<MenuCategory>> GetMenuCategory(
        [FromQuery] int categoryId,
        [HideFromOpenApi] string locale)
    {
        try
        {
            var category = await menuService.GetMenuCategory(locale, categoryId);

            if (category is null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource not found",
                    Detail = $"Resource with ID {categoryId} was not found."
                });
            }

            return Ok(category);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get menu category {CategoryId}", categoryId);
            return StatusCode(500);
        }
    }

    [HttpPost]
    [Route("categorydata")]
    [ExtractLocale]
    public async Task<ActionResult<MenuCategoryData>> GetMenuCategoryData(
        [FromBody] CategoryDataRequest filters, [HideFromOpenApi] string locale)
    {
        try
        {
            var match = Regex.Match(filters.Category, Constants.CategorySlugPattern);

            if (!match.Success)
            {
                return NotFound(ResponseHelper.NotFound());
            }

            var categorySlug = match.Groups[1].Value;

            if (!int.TryParse(match.Groups[2].Value, out var categoryId))
            {
                return NotFound(ResponseHelper.NotFound());
            }

            var categoryData =
                await menuService.GetCategoryData(locale, categoryId, categorySlug, filters.Filters, filters.Page);

            return Ok(categoryData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get category data for {Category}", filters.Category);
            return StatusCode(500);
        }
    }

    [HttpGet]
    [Route("recommendation")]
    [ExtractLocale]
    public async Task<ActionResult<IEnumerable<Product?>?>> GetRecommendation()
    {
        try
        {
            var products = await menuService.GetRecommendation();

            return Ok(products);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get recommendations");
            return StatusCode(500);
        }
    }

    [HttpGet]
    [Route("category-recommendation")]
    [ExtractLocale]
    public async Task<ActionResult<IEnumerable<object>?>> GetRecommendationByCategory(int categoryId)
    {
        try
        {
            var products = await menuService.GetRecommendationByCategory(categoryId);

            return Ok(products);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get recommendations for category {CategoryId}", categoryId);
            return StatusCode(500);
        }
    }
}
