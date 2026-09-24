using DMP.BL.Models.Support;
using DMP.DataAccess;
using DMP.DataAccess.Models;
using DMP.DataAccess.Models.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace DMP.BL.Services;

public class SupportService(IDbContextFactory<DmpDbContext> dmpContextFactory) : ISupportService
{
    public async Task<bool> CreateTicket(Guid userId, CreateTicketRequest request)
    {
        var newTicket = new SupportTicketDAL
        {
            UserId = userId,
            Type = request.Type,
            Subject = request.Subject,
            Status = SupportTicketStatus.New,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var message = request.Type == SupportTicketType.Payout
            ? $"{request.Payout!.Address}\n{request.Payout.Cryptocurrency}\n{request.Payout.Amount}\n{request.Message}"
            : request.Message;

        await using var context = await dmpContextFactory.CreateDbContextAsync();
        context.SupportTickets.Add(newTicket);

        if (request.Type == SupportTicketType.Payout)
        {
            context.Payouts.Add(new PayoutDAL
            {
                PayoutId = "01-" + Ulid.NewUlid(),
                UserId = userId,
                UserAmount = request.Payout!.Amount,
                Cryptocurrency = request.Payout.Cryptocurrency,
                Status = PayoutStatus.New,
                Address = request.Payout.Address,
            });
        }

        await context.SaveChangesAsync();

        context.SupportTicketMessages.Add(new SupportTicketMessageDAL
        {
            SupportTicketId = newTicket.SupportTicketId,
            SenderUserId = userId,
            Message = message,
            Type = SupportTicketMessageType.User
        });
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<GetSupportTicketsResponse> GetTickets(Guid userId, bool archived, int page, int pageSize = 5)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var ticketsQuery = context.SupportTickets.AsNoTracking()
            .Where(st =>
                st.UserId == userId && (archived
                    ? st.Status == SupportTicketStatus.Archived
                    : st.Status <= SupportTicketStatus.Completed));

        return new GetSupportTicketsResponse
        {
            TotalCount = await ticketsQuery.CountAsync(),
            Tickets = await ticketsQuery
                .OrderByDescending(st => st.SupportTicketId)
                .Skip(pageSize * page)
                .Take(pageSize)
                .Select(t => new SupportTicket
                {
                    SupportTicketId = t.SupportTicketId,
                    Type = t.Type,
                    Subject = t.Subject,
                    CreatedAt = t.CreatedAt,
                    Status = t.Status,
                    UpdatedAt = t.UpdatedAt
                })
                .ToListAsync()
        };
    }

    public async Task<SupportTicket?> GetTicket(Guid userId, int supportTicketId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var ticket = await context.SupportTickets.AsNoTracking()
            .Include(st => st.SupportTicketMessages)
            .FirstOrDefaultAsync(st => st.SupportTicketId == supportTicketId && st.UserId == userId);

        if (ticket is null)
        {
            return null;
        }

        var userIds = ticket.SupportTicketMessages.Select(c => c.SenderUserId).Distinct().ToList();
        var users = await context.Users
            .Where(u => userIds.Contains(u.UserId))
            .AsNoTracking()
            .ToDictionaryAsync(u => u.UserId, u => u.Name);

        return new SupportTicket
        {
            SupportTicketId = ticket.SupportTicketId,
            Type = ticket.Type,
            Subject = ticket.Subject,
            CreatedAt = ticket.CreatedAt,
            Status = ticket.Status,
            UpdatedAt = ticket.UpdatedAt,
            SupportTicketMessages = ticket.SupportTicketMessages.Select(stm => new SupportTicketMessage
            {
                SupportTicketMessageId = stm.SupportTicketMessageId,
                Message = stm.Message,
                CreatedAt = stm.CreatedAt,
                Type = stm.Type,
                UserName = users[stm.SenderUserId]
            }).ToList()
        };
    }

    public async Task<bool> AddMessage(Guid userId, AddTicketMessageRequest request)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var ticket = await context.SupportTickets
            .FirstOrDefaultAsync(st =>
                st.SupportTicketId == request.SupportTicketId && st.UserId == userId &&
                st.Status < SupportTicketStatus.Completed);

        if (ticket is null)
        {
            return false;
        }

        context.SupportTicketMessages.Add(new SupportTicketMessageDAL
        {
            SupportTicketId = ticket.SupportTicketId,
            SenderUserId = userId,
            Message = request.Message,
            Type = SupportTicketMessageType.User
        });

        ticket.UpdatedAt = DateTime.UtcNow;
        context.SupportTickets.Update(ticket);

        await context.SaveChangesAsync();

        return true;
    }
}
