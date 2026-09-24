using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models.Support;

public class SupportTicket
{
    public int SupportTicketId { get; set; }
    public SupportTicketType Type { get; set; }
    public string Subject { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public SupportTicketStatus Status { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<SupportTicketMessage>? SupportTicketMessages { get; set; }
}
