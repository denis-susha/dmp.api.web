namespace DMP.BL.Models.ProductCreation;

public class GetProductListResponse : PagedResponse
{
    public ICollection<ProductListItem>? Products { get; set; }
}
