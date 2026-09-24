namespace DMP.BL.Models;

public class ProductCategory
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = null!;
    public int? Order { get; set; }

    public IEnumerable<ProductCategory>? Children { get; set; }
}
