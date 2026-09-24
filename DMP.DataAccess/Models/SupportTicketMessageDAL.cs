using DMP.DataAccess.Models.Enumerations;

namespace DMP.DataAccess.Models;

public class SupportTicketMessageDAL
{
    public int SupportTicketMessageId { get; set; }
    public int SupportTicketId { get; set; }
    public Guid SenderUserId { get; set; }
    public string Message { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public SupportTicketMessageType Type { get; set; }

    public virtual SupportTicketDAL SupportTicket { get; set; } = null!;
}
