using DMP.DataAccess.Models.Enumerations;

namespace DMP.DataAccess.Models;

public class StoreDAL
{
    public Guid SellerId { get; set; }
    public string Name { get; set; } = null!;
    public string? CoverPath { get; set; }
    public StoreFlags Flags { get; set; }

    public virtual UserDAL Seller { get; set; } = null!;
}
