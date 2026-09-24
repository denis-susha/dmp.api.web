namespace DMP.BL.Models.ProductCreation;

public class GetProductDataItemsResponse
{
    public List<GetProductFilesResponse>? Files { get; set; }
    public bool IsUnlimited { get; set; }
    public bool IsLines { get; set; }
    public int Quantity { get; set; }
    public string[]? Lines { get; set; }
    public long FreeSpace { get; set; }
}
