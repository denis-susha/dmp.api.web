using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models;

public class CategoryFeature
{
    public int FeatureId { get; set; }
    public string Name { get; set; } = null!;
    public string Title { get; set; } = null!;
    public FeatureType Type { get; set; }
    public decimal? RangeMin { get; set; }
    public decimal? RangeMax { get; set; }
    public decimal? RangeFrom { get; set; }
    public decimal? RangeTo { get; set; }
    public bool? MultipleChoice { get; set; }

    public ICollection<CategoryFeatureCheckboxItem>? FilterCheckboxes { get; set; }
}
