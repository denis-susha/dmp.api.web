using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models.ProductCreation;

public class ProductListItem
{
    public int ProductId { get; set; }
    public int MenuCategoryId { get; set; }
    public int Quantity { get; set; }
    public bool Unlimited { get; set; }
    public decimal Price { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? ImgLink { get; set; }
    public ProductStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? MessageForSeller { get; set; }
}
