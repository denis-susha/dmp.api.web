using DMP.DataAccess.Models.Enumerations;

namespace DMP.DataAccess.Models;

public class SupportTicketDAL
{
    public int SupportTicketId { get; set; }
    public Guid UserId { get; set; }
    public SupportTicketType Type { get; set; }
    public string Subject { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public SupportTicketStatus Status { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public virtual ICollection<SupportTicketMessageDAL> SupportTicketMessages { get; set; } = null!;
}
