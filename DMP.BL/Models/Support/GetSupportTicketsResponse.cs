namespace DMP.BL.Models.Support;

public class GetSupportTicketsResponse
{
    public int TotalCount { get; set; }
    public ICollection<SupportTicket> Tickets { get; set; } = null!;
}
