namespace DMP.DataAccess.Models;

public class ProductImageDAL
{
    public int ProductId { get; set; }
    public string Hash { get; set; } = null!;
    public StoredImageDAL Images { get; set; } = null!;
    public bool IsCover { get; set; }
    public bool Attached { get; set; }
    public string Name { get; set; } = null!;

    public virtual ProductDAL? Product { get; set; }
}
