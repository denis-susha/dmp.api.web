using DMP.DataAccess.Models.Enumerations;

namespace DMP.DataAccess.Models;

public class ProductCreationDAL
{
    public int ProductCreationId { get; set; }
    public int ProductId { get; set; }
    public string? CustomCategory { get; set; }
    public bool UseCustomCategory { get; set; }
    public string[]? Lines { get; set; }
    /// <summary>Message from the admin to the seller (moderation feedback).</summary>
    public string? MessageForSeller { get; set; }
    /// <summary>Set when the product previously had the Ready status (was on sale).</summary>
    public ProductStatus? PreviousStatus { get; set; }

    public virtual ProductDAL? Product { get; set; }
}
