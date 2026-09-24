using DMP.BL.Models.ProductCreation;
using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models.Admin;

public class NewProductAdmin : NewProductRequest
{
    public string Slug { get; set; } = null!;
    public Guid SellerId { get; set; }
    public ProductStatus Status { get; set; }
    public List<GetProductFilesResponse>? Files { get; set; }
    public string[]? Lines { get; set; }
    public bool IsLines { get; set; }
    public int Quantity { get; set; }
}
