namespace DMP.BL.Models.Sales;

public class SalesViewProduct
{
    public int ProductId { get; set; }
    public string Slug { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? ImgLink { get; set; }
}
