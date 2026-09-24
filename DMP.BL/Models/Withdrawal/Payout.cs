using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models.Withdrawal;

public class Payout
{
    public string PayoutId { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal NetworkFee { get; set; }
    public Cryptocurrency Cryptocurrency { get; set; }
    public PayoutStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public decimal UserAmount { get; set; }
    public string Address { get; set; } = null!;
    public string? NetworkTransactionId { get; set; }
}
