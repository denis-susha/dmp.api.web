using DMP.DataAccess.Models.Enumerations;

namespace DMP.DataAccess.Models;

public class PageContentDAL
{
    public int PageContentId { get; set; }
    public Language Language { get; set; }
    public string Key { get; set; } = null!;
    public object? Content { get; set; }
}
