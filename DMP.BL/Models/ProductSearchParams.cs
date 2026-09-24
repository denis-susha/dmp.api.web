using DMP.BL.Models.Enumerations;

namespace DMP.BL.Models;

public class ProductSearchParams
{
    public string CategoryId { get; set; } = null!;
    public string RootCategoryId { get; set; } = null!;
    public ProductSorting SortBy { get; set; } = ProductSorting.New;
    public int PageSize { get; set; } = 10;
    public int PagingSize { get; set; } = 7;
    public int Page { get; set; } = 1;
    public Dictionary<string, string>? Filters { get; set; }
    public Dictionary<string, (string min, string max)>? Ranges { get; set; }
}
