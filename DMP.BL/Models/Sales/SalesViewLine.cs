namespace DMP.BL.Models.Sales;

public class SalesViewLine
{
    public int SalesLineId { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = null!;
    public int Quantity { get; set; }
    public string TransactionId { get; set; } = null!;

    public SalesViewProduct SaleViewProduct { get; set; } = null!;
}
