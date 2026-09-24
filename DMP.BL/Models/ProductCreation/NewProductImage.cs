namespace DMP.BL.Models.ProductCreation;

public class NewProductImage
{
    public string Hash { get; set; } = null!;
    public string? ImgUrl { get; set; }
    public bool IsCover { get; set; }
}
