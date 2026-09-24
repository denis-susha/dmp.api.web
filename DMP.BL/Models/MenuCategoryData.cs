namespace DMP.BL.Models;

public class MenuCategoryData
{
    public MenuCategory Category { get; set; } = null!;
    public List<Product>? Products { get; set; }
    public int TotalCount { get; set; }
}
