using API.Web.Attributes;
using DMP.BL.Models;
using DMP.BL.Models.Order;
using DMP.BL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[Authorize, ApiController]
[Route("[controller]")]
public class OrderController(ILogger<OrderController> logger, IOrderService orderService) : ControllerCustom(logger)
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder()
    {
        try
        {
            var result = await orderService.CreateOrder(UserId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create order");
            return StatusCode(500);
        }
    }

    [HttpGet("payment")]
    public async Task<ActionResult<GetPaymentResponse>> GetPayment([FromQuery] int orderId)
    {
        try
        {
            var result = await orderService.GetInvoice(UserId, orderId);

            if (result is null)
            {
                return BadRequest();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get payment for order {OrderId}", orderId);
            return StatusCode(500);
        }
    }

    [ExtractLocale]
    [HttpGet]
    public async Task<ActionResult<GetOrderResponse>> GetOrder([FromQuery] int orderId, [HideFromOpenApi] string locale)
    {
        try
        {
            var result = await orderService.GetOrder(UserId, orderId, locale);

            if (result is null)
            {
                return NotFound();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get order {OrderId}", orderId);
            return StatusCode(500);
        }
    }

    [HttpPost("list")]
    public async Task<ActionResult<GetOrderListResponse>> GetOrderList([FromBody] TableQuery request)
    {
        try
        {
            var result = await orderService.GetOrderList(UserId, request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get order list");
            return StatusCode(500);
        }
    }

    [HttpPost("setpaymentdetails")]
    public async Task<IActionResult> SetPaymentDetails([FromBody] SetPaymentDetailsRequest request)
    {
        try
        {
            var validationResult = request.Validate();

            if (!validationResult.IsValid)
            {
                Logger.LogError("Invalid SetPaymentDetailsRequest: {Request}. Error: {ErrorMsg}", request,
                    validationResult.ErrorMsg);

                return BadRequest();
            }

            var result = await orderService.SetPaymentDetails(UserId, request.PaymentMethodId, request.OrderId,
                request.Address);

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to set payment details for order {OrderId}", request.OrderId);
            return StatusCode(500);
        }
    }
}
