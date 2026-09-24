using System.Text.Json.Serialization;

namespace DMP.BL.Models;

public class MenuCategory
{
    public int MenuCategoryId { get; set; }
    public short? MenuId { get; set; }
    [JsonIgnore]
    public string Name { get; set; } = null!;
    public string Title { get; set; } = null!;
    public int? ParentId { get; set; }
    public int? Order { get; set; }
    [JsonIgnore]
    public string Slug { get; set; } = null!;
    public string Url { get; set; } = null!;

    public MenuCategory? Parent { get; set; }

    public IEnumerable<MenuCategory>? Children { get; set; }
    public IEnumerable<CategoryFeature>? Features { get; set; }
}
