namespace DMP.BL.Models.Store;

public class ProductStoreInfo
{
    public Guid StoreId { get; set; }
    public string Name { get; set; } = null!;
    public string? CoverPath { get; set; }
}
