using DMP.BL.Models.Enumerations;
using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models.Order;

public class GetPaymentResponse
{
    public string CreatedAt { get; set; } = null!;
    public decimal Price { get; set; }
    public Currency Currency { get; set; }
    public int Expiration { get; set; }
    public List<PaymentMethod> PaymentMethods { get; set; } = null!;
    public PaymentStatus Status { get; set; }
    public int OrderId { get; set; }
    public Cryptocurrency? PaidCryptocurrency { get; set; }
    public decimal? SentAmount { get; set; }
}
