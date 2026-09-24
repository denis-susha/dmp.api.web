using DMP.BL.Models;
using DMP.BL.Models.Withdrawal;
using DMP.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace DMP.BL.Services;

public class WithdrawalService(IDbContextFactory<DmpDbContext> dmpContextFactory) : IWithdrawalService
{
    public async Task<GetPayoutsResponse> GetPayoutsList(Guid userId, TableQuery request)
    {
        var pageSize = request.Pagination.PageSize ?? 10;

        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var payoutsQuery = context.Payouts.AsNoTracking()
            .Where(p => p.UserId == userId);

        var totalCount = await payoutsQuery.CountAsync();

        var payoutsDal = await payoutsQuery
            .OrderByDescending(p => p.PayoutId)
            .Skip(request.Pagination.Page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new GetPayoutsResponse
        {
            TotalCount = totalCount,
            Payouts = payoutsDal.Select(p => new Payout
            {
                PayoutId = p.PayoutId,
                Amount = p.Amount,
                NetworkFee = p.NetworkFee,
                Cryptocurrency = p.Cryptocurrency,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                UserAmount = p.UserAmount,
                Address = p.Address,
                NetworkTransactionId = p.NetworkTransactionId
            }).ToList()
        };
    }
}
