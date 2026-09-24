using DMP.BL.Models;
using DMP.BL.Models.Order;

namespace DMP.BL.Services;

public interface IOrderService
{
    Task<CreateOrderResponse> CreateOrder(Guid userId);
    Task<GetPaymentResponse?> GetInvoice(Guid userId, int orderId);
    Task<GetOrderResponse?> GetOrder(Guid userId, int orderId, string lng);
    Task<bool> SetPaymentDetails(Guid userId, string bcPaymentMethodId, int orderId, string address);
    Task<GetOrderListResponse> GetOrderList(Guid userId, TableQuery request);
}
