using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models.ProductCreation;

public class ProductView
{
    public int ProductId { get; set; }
    public int? MenuCategoryId { get; set; }
    public bool UseCustomCategory { get; set; }
    public string? CustomCategory { get; set; }
    public ProductStatus? PreviousStatus { get; set; }
    public UserFeature UserFeatures { get; set; } = null!;
    public Dictionary<int, string>? Features { get; set; }
    public decimal Price { get; set; }
    public IEnumerable<NewProductImage>? ProductImages { get; set; }
    public GetProductDataItemsResponse? ProductDataItems { get; set; }
    public string? MessageForSeller { get; set; }
}
