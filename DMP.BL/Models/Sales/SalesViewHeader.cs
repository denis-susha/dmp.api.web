using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models.Sales;

public class SalesViewHeader
{
    public int SalesHeaderId { get; set; }
    public decimal Amount { get; set; }
    public Cryptocurrency Cryptocurrency { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<SalesViewLine> SalesViewLines { get; set; } = null!;
}
