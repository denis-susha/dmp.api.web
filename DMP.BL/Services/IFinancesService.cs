using DMP.BL.Models.Finances;

namespace DMP.BL.Services;

public interface IFinancesService
{
    Task<List<AccountBalance>> GetAccountsBalances(Guid userId);
    Task<decimal> GetBalance(Guid userId);
    Task<GetFinancesResponse> GetFinances(Guid userId);
}
