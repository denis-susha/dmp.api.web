using DMP.BL.Models.Support;

namespace DMP.BL.Services;

public interface ISupportService
{
    Task<bool> CreateTicket(Guid userId, CreateTicketRequest request);
    Task<GetSupportTicketsResponse> GetTickets(Guid userId, bool archived, int page, int pageSize = 5);
    Task<SupportTicket?> GetTicket(Guid userId, int supportTicketId);
    Task<bool> AddMessage(Guid userId, AddTicketMessageRequest request);
}
