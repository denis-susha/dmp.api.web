namespace DMP.BL.Models.Dashboard;

public class TopSellProduct
{
    public int ProductId { get; set; }
    public string Name { get; set; } = null!;
    public long TotalSold { get; set; }
    public string Slug { get; set; } = null!;
    public string? ImgLink { get; set; }
    public decimal Price { get; set; }
}
