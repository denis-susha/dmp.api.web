using System.Text.RegularExpressions;
using API.Web.Attributes;
using API.Web.Helpers;
using DMP.BL.Models;
using DMP.BL.Models.Favorites;
using DMP.BL.Models.Search;
using DMP.BL.Models.Store;
using DMP.BL.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductController(ILogger<ProductController> logger, IProductService productService) : ControllerBase
{
    [HttpGet]
    [Route("product")]
    [ExtractLocale]
    public async Task<ActionResult<ProductData>> GetProduct([FromQuery] string product, string locale)
    {
        try
        {
            var match = Regex.Match(product, Constants.ProductSlugPattern);

            if (!match.Success)
            {
                return NotFound(ResponseHelper.NotFound());
            }

            var productSlug = match.Groups[1].Value;

            if (!int.TryParse(match.Groups[2].Value, out var productId))
            {
                return NotFound(ResponseHelper.NotFound());
            }

            var result = await productService.GetProductData(locale, productId, productSlug);

            if (result is null)
            {
                return NotFound(ResponseHelper.NotFound($"Resource with ID {product} was not found."));
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get product {Product}", product);
            return StatusCode(500);
        }
    }

    [HttpGet]
    [Route("search")]
    [ExtractLocale]
    public async Task<ActionResult<FullTextSearchProductsResponse>> SearchProducts([FromQuery] string query,
        [HideFromOpenApi] string locale = "")
    {
        try
        {
            if (query.Length < 3)
            {
                return BadRequest();
            }

            var result = await productService.SearchProducts(query, locale);

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to search products for {Query}", query);
            return StatusCode(500);
        }
    }

    [HttpPost]
    [Route("search-products")]
    [ExtractLocale]
    public async Task<ActionResult<FullTextSearchResponse>> FullTextSearchProducts([FromBody] FullTextSearchProductsRequest request,
        [HideFromOpenApi] string locale)
    {
        try
        {
            if (request.Query.Length < 3)
            {
                return BadRequest();
            }

            var result = await productService.FullTextSearchProducts(request.Query, request.Pagination, locale);

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to run full-text product search for {Query}", request.Query);
            return StatusCode(500);
        }
    }

    [HttpPost("favorites")]
    public async Task<ActionResult<FavoritesList>> GetFavoritesList([FromBody] int[] productIds)
    {
        try
        {
            if (productIds.Length is 0 or > 10)
            {
                logger.LogError("Invalid productIds.");
                return BadRequest();
            }

            var result = await productService.GetFavoritesProducts(productIds);

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get favorite products");
            return StatusCode(500);
        }
    }

    [HttpGet]
    [Route("store")]
    [ExtractLocale]
    public async Task<ActionResult<ProductStoreInfo?>> GetProductStoreInfo([FromQuery] int productId)
    {
        try
        {
            var result = await productService.GetProductStoreInfo(productId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get store info for product {ProductId}", productId);
            return StatusCode(500);
        }
    }
}
