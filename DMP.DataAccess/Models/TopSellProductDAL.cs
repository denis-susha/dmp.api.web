namespace DMP.DataAccess.Models;

public class TopSellProductDAL
{
    public long SalesQuantity { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? ImgLink { get; set; }
    public decimal Price { get; set; }
}
