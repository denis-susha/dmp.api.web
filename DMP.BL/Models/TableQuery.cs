namespace DMP.BL.Models;

public class TableQuery
{
    public PaginationModel Pagination { get; set; } = null!;
    public ICollection<SortModel>? Sorting { get; set; }
    public ICollection<FilterModel>? Filters { get; set; }
}

public class SortModel
{
    public string Field { get; set; } = null!;
    public string Sort { get; set; } = null!;
}

public class PaginationModel
{
    public int Page { get; set; }
    public int? PageSize { get; set; }
}

public class FilterModel
{
    public string Field { get; set; } = null!;
    public string Value { get; set; } = null!;
}
