namespace DMP.BL.Models.Withdrawal;

public class GetPayoutsResponse : PagedResponse
{
    public ICollection<Payout> Payouts { get; set; } = null!;
}
