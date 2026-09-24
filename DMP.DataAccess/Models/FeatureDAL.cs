using DMP.DataAccess.Models.Enumerations;

namespace DMP.DataAccess.Models;

public class FeatureDAL
{
    public int FeatureId { get; set; }
    public string Name { get; set; } = null!;
    public FeatureType Type { get; set; }
    public string[]? CheckboxesValues { get; set; }
    public decimal? RangeMin { get; set; }
    public decimal? RangeMax { get; set; }
    public decimal? RangeFrom { get; set; }
    public decimal? RangeTo { get; set; }
    public bool? MultipleChoice { get; set; }
    public FeatureRangeType? RangeType { get; set; }

    public virtual ICollection<ProductFeatureDAL> ProductFeatures { get; set; } = null!;
    public virtual ICollection<CategoryFeatureDAL> CategoryFeatures { get; set; } = null!;
}
