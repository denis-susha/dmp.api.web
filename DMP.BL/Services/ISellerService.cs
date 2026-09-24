using DMP.BL.Models.Dashboard;
using DMP.BL.Models.Enumerations;
using DMP.BL.Models.Seller;

namespace DMP.BL.Services;

public interface ISellerService
{
    Task<DashboardInfo> GetDashboardInfo(Guid userId, Period period);
    Task<GetSellerUserResponse> GetSellerUser(Guid userId);
}
