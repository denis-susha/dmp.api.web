using System.Text;
using System.Text.Json;
using DMP.BL.Constants;
using DMP.BL.Helpers;
using DMP.BL.Models;
using DMP.BL.Models.Bitcart;
using DMP.BL.Models.Enumerations;
using DMP.BL.Models.Order;
using DMP.DataAccess;
using DMP.DataAccess.Models;
using DMP.DataAccess.Models.Enumerations;
using DMP.DataAccess.Models.Order;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMP.BL.Services;

public class OrderService(
    ILogger<OrderService> logger,
    IRedisService redisService,
    IDbContextFactory<DmpDbContext> dmpContextFactory,
    IHttpClientFactory httpClientFactory,
    IApplicationSettingsService applicationSettingsService,
    IProductService productService) : IOrderService
{
    private const string MediaType = "application/json";
    private const string BitcartClientName = "BitcartBackend";

    public async Task<CreateOrderResponse> CreateOrder(Guid userId)
    {
        var result = new CreateOrderResponse
        {
            Status = CreateOrderStatus.Success
        };

        int orderId;
        var amount = 0m;
        var affectedProductIds = new List<int>();
        var newOrderLines = new List<OrderLineDAL>();
        await using (var context = await dmpContextFactory.CreateDbContextAsync())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var items = await context.CartItems.Where(ci => ci.UserId == userId && ci.Selected).AsNoTracking().ToListAsync();

                foreach (var item in items)
                {
                    var product = await context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == item.ProductId);
                    if (product is null || !product.CheckProductAvailableQty(item.Quantity))
                    {
                        result.Status = CreateOrderStatus.OutdatedData;
                        return result;
                    }

                    if (!product.Unlimited)
                    {
                        affectedProductIds.Add(product.ProductId);
                    }

                    newOrderLines.Add(new OrderLineDAL
                    {
                        ProductId = product.ProductId,
                        SellerId = product.SellerId,
                        Price = product.Price,
                        Currency = BlConstants.DefaultCurrency,
                        Quantity = item.Quantity,
                        IsLine = product.IsLines
                    });

                    amount += product.Price * item.Quantity;
                }

                var newOrderHdr = new OrderHeaderDAL
                {
                    UserId = userId,
                    Amount = amount,
                    Currency = BlConstants.DefaultCurrency,
                };

                context.Add(newOrderHdr);
                await context.SaveChangesAsync();
                orderId = newOrderHdr.OrderId;

                newOrderLines.ForEach(nol => nol.OrderId = orderId);

                // Re-read limited products with a row lock so the quantity check and decrement are atomic.
                List<ProductDAL> lockedProducts = [];
                if (affectedProductIds.Count > 0)
                {
                    var productIds = affectedProductIds.ToArray();
                    lockedProducts = await context.Products
                        .FromSql($"SELECT * FROM public.\"Product\" WHERE \"ProductId\" = ANY({productIds}) FOR UPDATE")
                        .ToListAsync();
                }

                foreach (var newOrderLine in newOrderLines)
                {
                    var lockedProduct = lockedProducts.FirstOrDefault(lp => lp.ProductId == newOrderLine.ProductId);
                    if (lockedProduct is not null)
                    {
                        if (!lockedProduct.CheckProductAvailableQty(newOrderLine.Quantity))
                        {
                            throw new Exception(
                                $"The product {newOrderLine.ProductId} quantity has become less ({lockedProduct.Quantity}) than needed ({newOrderLine.Quantity}) for the order line at order creation time.");
                        }

                        if (newOrderLine.IsLine)
                        {
                            var productId = newOrderLine.ProductId;
                            var lockedProductLine = await context.ProductLines
                                .FromSql($"SELECT * FROM public.\"ProductLine\" WHERE \"ProductId\" = {productId} AND \"IsSold\" = false FOR UPDATE")
                                .FirstOrDefaultAsync()
                                ?? throw new Exception($"Can't find ProductLine for ProductId={newOrderLine.ProductId}");

                            lockedProductLine.IsSold = true;
                            context.ProductLines.Update(lockedProductLine);

                            newOrderLine.ProductLineId = lockedProductLine.ProductLineId;
                        }

                        lockedProduct.Quantity -= newOrderLine.Quantity;
                        context.Update(lockedProduct);
                    }

                    if (!newOrderLine.IsLine)
                    {
                        newOrderLine.ProductFileId = await context.ProductFiles
                            .Where(pf => pf.ProductId == newOrderLine.ProductId && pf.Attached && pf.IsUploaded)
                            .Select(pf => pf.ProductFileId)
                            .FirstAsync();
                    }

                    context.Add(newOrderLine);
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                logger.LogError(ex, "Failed to create order for user {UserId}", userId);

                result.Status = CreateOrderStatus.OutdatedData;
                return result;
            }
        }

        if (affectedProductIds.Count > 0)
        {
            await productService.UpdateCacheProducts(affectedProductIds);
        }

        var bitcartSettings = await applicationSettingsService.GetBitcartSettings();
        var requestBc = new CreateInvoiceRequestBC
        {
            currency = BlConstants.DefaultCurrency.ToString(),
            price = amount.ToString(BlConstants.CurrencyFormat),
            store_id = bitcartSettings.StoreId,
            expiration = 15
        };
        var jsonRequest = JsonSerializer.Serialize(requestBc, BlConstants.BcJsonSerializerOptions);
        logger.LogDebug("Invoice creation request for BitcartBackend: {Request}", jsonRequest);

        InvoiceResponseBC response;
        try
        {
            var client = httpClientFactory.CreateClient(BitcartClientName);
            using var content = new StringContent(jsonRequest, Encoding.UTF8, MediaType);
            using var reply = await client.PostAsync("invoices", content);
            var resultContent = await reply.Content.ReadAsStringAsync();

            if (!reply.IsSuccessStatusCode)
            {
                throw new Exception($"Bitcart error. Bitcart response: {resultContent}");
            }

            response = JsonSerializer.Deserialize<InvoiceResponseBC>(resultContent)!;

            logger.LogDebug("Received invoice response from BitcartBackend: {Response}", resultContent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create Bitcart invoice for order {OrderId}", orderId);
            throw;
        }

        var cartItemsToDelete = newOrderLines
            .Select(orderLine => new CartItemDAL { UserId = userId, ProductId = orderLine.ProductId })
            .ToList();

        await using (var context = await dmpContextFactory.CreateDbContextAsync())
        {
            context.CartItems.RemoveRange(cartItemsToDelete);
            await context.SaveChangesAsync();
        }

        var paymentMethods = response.payments.Select(p => new PaymentMethodDAL
        {
            OrderId = orderId,
            BcPaymentMethodId = p.id,
            UserId = userId
        });

        await using (var context = await dmpContextFactory.CreateDbContextAsync())
        {
            context.PaymentMethods.AddRange(paymentMethods);

            var orderHdr = await context.OrderHeaders.FirstAsync(oh => oh.OrderId == orderId);
            orderHdr.ReferenceNumber = response.id;
            context.Update(orderHdr);

            context.InvoiceWorkerTasks.Add(new InvoiceWorkerTaskDAL
            {
                InvoiceId = response.id,
                Status = InvoiceWorkerTaskStatus.New,
                OrderId = orderId
            });

            await context.SaveChangesAsync();
        }

        await SendSysNotify(orderId);

        result.OrderId = orderId;

        return result;
    }

    private async Task SendSysNotify(int orderId)
    {
        try
        {
            await redisService.AddSysNotification($"New order created.\nOrderId: {orderId}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send system notification for new order {OrderId}", orderId);
        }
    }

    public async Task<GetPaymentResponse?> GetInvoice(Guid userId, int orderId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var orderHdr = await context.OrderHeaders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

        if (orderHdr is null || string.IsNullOrEmpty(orderHdr.ReferenceNumber))
        {
            return null;
        }

        InvoiceResponseBC response;
        try
        {
            var client = httpClientFactory.CreateClient(BitcartClientName);
            using var reply = await client.GetAsync($"invoices/{orderHdr.ReferenceNumber}");
            var resultContent = await reply.Content.ReadAsStringAsync();
            response = JsonSerializer.Deserialize<InvoiceResponseBC>(resultContent)!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get Bitcart invoice {InvoiceId} for order {OrderId}", orderHdr.ReferenceNumber, orderId);
            throw;
        }

        var createdAt = DateTime.Parse(response.created).ToUniversalTime().ToString("O");
        var currency = Enum.Parse<Currency>(response.currency, ignoreCase: true);
        Cryptocurrency? paidCurrency = string.IsNullOrEmpty(response.paid_currency)
            ? null
            : Enum.Parse<Cryptocurrency>(response.paid_currency, ignoreCase: true);

        return new GetPaymentResponse
        {
            CreatedAt = createdAt,
            Price = decimal.Parse(response.price),
            Currency = currency,
            Expiration = response.expiration,
            PaymentMethods = response.payments.Select(p => new PaymentMethod
            {
                PaymentMethodId = p.id,
                Rate = decimal.Parse(p.rate),
                Cryptocurrency = ExtractCryptocurrency(p.currency, p.symbol),
                PaymentAddress = p.payment_address,
                Divisibility = p.divisibility,
                PaymentUrl = p.payment_url,
                RecommendedFee = p.recommended_fee,
                Lightning = p.lightning,
                Amount = decimal.Parse(p.amount),
                NodeId = p.node_id,
                UserAddress = p.user_address,
            }).ToList(),
            Status = OrderStatusToPaymentStatus(orderHdr.Status),
            OrderId = orderHdr.OrderId,
            PaidCryptocurrency = paidCurrency,
            SentAmount = response.sent_amount
        };
    }

    private Cryptocurrency ExtractCryptocurrency(string platform, string token) =>
        applicationSettingsService.CryptocurrencySettings
            .First(cs => cs.PlatformCode == platform.ToUpper() && cs.TokenCode == token.ToUpper())
            .CryptocurrencyId;

    public async Task<bool> SetPaymentDetails(Guid userId, string bcPaymentMethodId, int orderId, string address)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var isPaymentMethodExists = await context.PaymentMethods.AnyAsync(o =>
            o.BcPaymentMethodId == bcPaymentMethodId && o.OrderId == orderId && o.UserId == userId);

        if (!isPaymentMethodExists)
        {
            return false;
        }

        var orderHdr = await context.OrderHeaders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

        if (orderHdr is null || string.IsNullOrEmpty(orderHdr.ReferenceNumber))
        {
            return false;
        }

        var requestBc = new UpdatePaymentMethodDetailsRequestBC
        {
            id = bcPaymentMethodId,
            address = address
        };

        var jsonRequest = JsonSerializer.Serialize(requestBc, BlConstants.JsonSerializerOptions);
        logger.LogDebug("Payment method update request for BitcartBackend: {Request}", jsonRequest);

        try
        {
            var client = httpClientFactory.CreateClient(BitcartClientName);
            using var content = new StringContent(jsonRequest, Encoding.UTF8, MediaType);
            using var reply = await client.PatchAsync($"invoices/{orderHdr.ReferenceNumber}/details", content);

            return reply.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update Bitcart payment method {PaymentMethodId} for order {OrderId}", bcPaymentMethodId, orderId);
            throw;
        }
    }

    public async Task<GetOrderResponse?> GetOrder(Guid userId, int orderId, string lng)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var orderHdr = await context.OrderHeaders.AsNoTracking()
            .Include(h => h.OrderLines)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

        if (orderHdr is null || string.IsNullOrEmpty(orderHdr.ReferenceNumber))
        {
            return null;
        }

        var result = new GetOrderResponse
        {
            CreatedAt = orderHdr.CreatedAt.ToString("O"),
            Amount = orderHdr.Amount,
            Currency = orderHdr.Currency,
            Status = OrderStatusToPaymentStatus(orderHdr.Status),
            OrderId = orderHdr.OrderId,
            Lines = []
        };

        foreach (var orderLineDAL in orderHdr.OrderLines!)
        {
            var (product, _) = await redisService.SearchProduct(orderLineDAL.ProductId);

            if (product is null)
            {
                throw new Exception($"Product {orderLineDAL.ProductId} is not found.");
            }

            var storeDal = await context.Stores.AsNoTracking().FirstAsync(s => s.SellerId == product.SellerId);

            var line = new GetOrderLineResponse
            {
                Slug = product.Slug,
                Cover = product.ImgLinks is { Length: > 0 } ? product.ImgLinks[0] : null,
                Price = orderLineDAL.Price,
                ProductName = product.UserFeatures!.Name,
                Quantity = orderLineDAL.Quantity,
                ProductId = orderLineDAL.ProductId,
                StoreName = storeDal.Name,
                FeaturesValues = await productService.GetProductFeaturesValues(product, lng)
            };

            if (result.Status == PaymentStatus.Complete)
            {
                line.Data = new GetOrderLineDataResponse
                {
                    IsLine = orderLineDAL.IsLine
                };

                if (line.Data.IsLine)
                {
                    var productLine = await context.ProductLines.FirstAsync(pl => pl.ProductLineId == orderLineDAL.ProductLineId);
                    line.Data.LineData = productLine.Value;
                }
                else
                {
                    var productFile = await context.ProductFiles.FirstAsync(pf => pf.ProductFileId == orderLineDAL.ProductFileId);

                    line.Data.FileSize = productFile.FileSize;
                    line.Data.FileName = productFile.FileName;
                    line.Data.FileLineIdentificator = orderLineDAL.OrderLineId;
                }
            }

            result.Lines.Add(line);
        }

        return result;
    }

    public async Task<GetOrderListResponse> GetOrderList(Guid userId, TableQuery request)
    {
        var pageSize = request.Pagination.PageSize ?? 10;
        var page = request.Pagination.Page - 1;
        await using var context = await dmpContextFactory.CreateDbContextAsync();

        var query = context.OrderHeaders.AsNoTracking().Where(o => o.UserId == userId);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip(page * pageSize).Take(pageSize)
            .ToListAsync();

        return new GetOrderListResponse
        {
            TotalCount = await query.CountAsync(),
            Orders = orders.Select(o => new GetOrderResponse
            {
                CreatedAt = o.CreatedAt.ToString("O"),
                Currency = o.Currency,
                Amount = o.Amount,
                Status = OrderStatusToPaymentStatus(o.Status),
                OrderId = o.OrderId
            }).ToList()
        };
    }

    private static PaymentStatus OrderStatusToPaymentStatus(OrderStatus orderStatus) => orderStatus switch
    {
        OrderStatus.New or OrderStatus.Paid => PaymentStatus.InProgress,
        OrderStatus.PaidOver or OrderStatus.Complete => PaymentStatus.Complete,
        OrderStatus.PaidPartial => PaymentStatus.PaidPartial,
        OrderStatus.Expired => PaymentStatus.Expired,
        OrderStatus.Refunded => PaymentStatus.Refunded,
        OrderStatus.ErrorOnCreation or OrderStatus.PaidError => PaymentStatus.PaidError,
        _ => throw new ArgumentOutOfRangeException(nameof(orderStatus), orderStatus, null)
    };
}
