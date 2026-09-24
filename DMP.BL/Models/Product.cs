namespace DMP.BL.Models;

public class Product
{
    public int ProductId { get; set; }
    public string MenuCategoryId { get; set; } = null!;
    public UserFeature? UserFeatures { get; set; }
    public string Slug { get; set; } = null!;
    public Guid SellerId { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public bool Unlimited { get; set; }
    public string[]? ImgLinks { get; set; }
    public List<ProductFeature> Features { get; set; } = null!;
    public bool IsLines { get; set; }
    public long CreatedAt { get; set; }

    public int? Duration { get; set; }
    public int? Year { get; set; }
}
