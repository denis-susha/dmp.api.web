namespace DMP.BL.Models.ProductCreation;

public class GenerateUploadUrlResponse
{
    public GenerateUploadUrlStatus Status { get; set; }
    public string? PresignedUrl { get; set; }
    public int? ProductFileId { get; set; }
}
