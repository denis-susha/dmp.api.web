using API.Web.Attributes;
using DMP.BL.Models.Cart;
using DMP.BL.Models.Enumerations;
using DMP.BL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[Authorize, ApiController]
[Route("[controller]")]
public class CartController(ILogger<CartController> logger, ICartService cartService) : ControllerCustom(logger)
{
    [ExtractLocale]
    [HttpGet]
    public async Task<ActionResult<CartData>> GetCartData([HideFromOpenApi] string locale, bool isCheckout = false)
    {
        try
        {
            var result = await cartService.GetCartData(UserId, isCheckout, locale);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get cart data");
            return StatusCode(500);
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        try
        {
            var validationResult = request.Validate();

            if (!validationResult.IsValid)
            {
                return BadRequest();
            }

            var result = await cartService.AddToCart(UserId, request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to add item to cart");
            return StatusCode(500);
        }
    }

    [ExtractLocale]
    [HttpPost("update")]
    public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemRequest request, [HideFromOpenApi] string locale = "")
    {
        try
        {
            if (request.Quantity is < 1 or > 2000)
            {
                return BadRequest();
            }

            var userId = UserId;
            var result = new UpdateCartItemResponse
            {
                Status = await cartService.UpdateItem(userId, request)
            };

            if (result.Status == UpdateCartStatus.Success)
            {
                result.CartData = await cartService.GetCartData(userId, false, locale);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update cart item");
            return StatusCode(500);
        }
    }

    [ExtractLocale]
    [HttpPost("select")]
    public async Task<IActionResult> SelectCartItems([FromBody] CartItemSelection[] selections, [HideFromOpenApi] string locale = "")
    {
        try
        {
            var userId = UserId;
            await cartService.SelectCartItems(userId, [.. selections]);
            var result = await cartService.GetCartData(userId, false, locale);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to select cart items");
            return StatusCode(500);
        }
    }

    [ExtractLocale]
    [HttpPost("delete")]
    public async Task<IActionResult> Remove([FromBody] int[] productId, [HideFromOpenApi] string locale = "")
    {
        try
        {
            var userId = UserId;
            var result = new UpdateCartItemResponse
            {
                Status = await cartService.RemoveItems(userId, productId)
            };

            if (result.Status == UpdateCartStatus.Success)
            {
                result.CartData = await cartService.GetCartData(userId, false, locale);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to remove cart items");
            return StatusCode(500);
        }
    }
}
