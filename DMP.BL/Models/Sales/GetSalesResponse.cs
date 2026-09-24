namespace DMP.BL.Models.Sales;

public class GetSalesResponse : PagedResponse
{
    public ICollection<Sale> Sales { get; set; } = null!;
}
