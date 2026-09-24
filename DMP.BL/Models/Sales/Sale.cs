using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models.Sales;

public class Sale
{
    public int SalesHeaderId { get; set; }
    public decimal Amount { get; set; }
    public Cryptocurrency Cryptocurrency { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
