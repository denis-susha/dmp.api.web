using DMP.BL.Models.Enumerations;
using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models.Order;

public class GetOrderResponse
{
    public string CreatedAt { get; set; } = null!;
    public Currency Currency { get; set; }
    public PaymentStatus Status { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }

    public List<GetOrderLineResponse> Lines { get; set; } = null!;
}
