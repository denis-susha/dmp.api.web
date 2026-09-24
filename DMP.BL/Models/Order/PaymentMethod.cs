using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models.Order;

public class PaymentMethod
{
    public string PaymentMethodId { get; set; } = null!;
    public decimal Rate { get; set; }
    public Cryptocurrency Cryptocurrency { get; set; }
    public string PaymentAddress { get; set; } = null!;
    public int Divisibility { get; set; }
    public string PaymentUrl { get; set; } = null!;
    public decimal RecommendedFee { get; set; }
    public bool Lightning { get; set; }
    public decimal Amount { get; set; }
    public string? NodeId { get; set; }
    public string? UserAddress { get; set; }
}
