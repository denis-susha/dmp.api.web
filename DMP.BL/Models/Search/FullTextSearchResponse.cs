namespace DMP.BL.Models.Search;

public class FullTextSearchResponse
{
    public List<Product>? Products { get; set; }
    public long TotalCount { get; set; }
}
