namespace DMP.BL.Models.ProductCreation;

public class UploadProductImageResponse
{
    public NewProductImage Image { get; set; } = null!;
    public CreateOrUpdateProductStatus Status { get; set; }
}
