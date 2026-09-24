namespace DMP.DataAccess.Models;

public class CategoryFeatureDAL
{
    public int CategoryId { get; set; }
    public int FeatureId { get; set; }

    public virtual FeatureDAL? Feature { get; set; }
    public virtual MenuCategoryDAL MenuCategory { get; set; } = null!;
}
