namespace DMP.BL.Models.Cart;

public class CartItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public bool Selected { get; set; }
    public decimal Price { get; set; }
    public decimal Amount { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? ImgLink { get; set; }
    public string FeaturesValues { get; set; } = null!;
}
