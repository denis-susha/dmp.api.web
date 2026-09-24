namespace DMP.BL.Models.ProductCreation;

public class NewProductFile
{
    public byte[] Data { get; set; } = null!;
    public string Extension { get; set; } = null!;
    public string Quality { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string StoredPath { get; set; } = null!;
}
