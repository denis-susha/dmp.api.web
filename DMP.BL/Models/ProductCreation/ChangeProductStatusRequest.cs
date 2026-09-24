using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models.ProductCreation;

public class ChangeProductStatusRequest
{
    public int ProductId { get; set; }
    public ProductStatus NewStatus { get; set; }
}
