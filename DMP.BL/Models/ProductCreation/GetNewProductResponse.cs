namespace DMP.BL.Models.ProductCreation;

public class GetNewProductResponse
{
    public GetNewProductRequestStatus Status { get; set; }
    public NewProductRequest? Product { get; set; }
}
