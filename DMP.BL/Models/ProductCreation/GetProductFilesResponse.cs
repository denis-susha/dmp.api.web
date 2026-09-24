namespace DMP.BL.Models.ProductCreation;

public class GetProductFilesResponse
{
    public int ProductFileId { get; set; }
    public long FileSize { get; set; }
    public string FileName { get; set; } = null!;
}
