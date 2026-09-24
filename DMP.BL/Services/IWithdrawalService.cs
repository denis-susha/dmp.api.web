using DMP.BL.Models;
using DMP.BL.Models.Withdrawal;

namespace DMP.BL.Services;

public interface IWithdrawalService
{
    Task<GetPayoutsResponse> GetPayoutsList(Guid userId, TableQuery request);
}
