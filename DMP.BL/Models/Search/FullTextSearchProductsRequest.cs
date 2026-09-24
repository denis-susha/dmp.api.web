namespace DMP.BL.Models.Search;

public class FullTextSearchProductsRequest
{
    public string Query { get; set; } = null!;
    public PaginationModel Pagination { get; set; } = null!;
}
