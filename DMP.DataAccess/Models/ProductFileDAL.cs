namespace DMP.DataAccess.Models;

public class ProductFileDAL
{
    public int ProductFileId { get; set; }
    public int ProductId { get; set; }
    public Guid UserId { get; set; }
    public long FileSize { get; set; }
    public string FileName { get; set; } = null!;
    public string StoragePath { get; set; } = null!;
    public bool IsUploaded { get; set; }
    public bool Attached { get; set; }

    public virtual ProductDAL? Product { get; set; }
}
