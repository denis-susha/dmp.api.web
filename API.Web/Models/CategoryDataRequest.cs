namespace API.Web.Models;

public class CategoryDataRequest
{
    public string Category { get; set; } = null!;
    public int Page { get; set; }
    public Dictionary<string, string> Filters { get; set; } = null!;
}
