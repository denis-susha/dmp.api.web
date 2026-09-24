using DMP.BL.Models.Enumerations;

namespace DMP.BL.Models.Order;

public class CreateOrderResponse
{
    public CreateOrderStatus Status { get; set; }
    public int? OrderId { get; set; }
}
