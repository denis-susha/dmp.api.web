using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models.Support;

public class SupportTicketMessage
{
    public int SupportTicketMessageId { get; set; }
    public string Message { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public SupportTicketMessageType Type { get; set; }
    public string UserName { get; set; } = null!;
}
