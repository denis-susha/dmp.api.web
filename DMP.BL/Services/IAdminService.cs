using DMP.BL.Models.Admin;

namespace DMP.BL.Services;

public interface IAdminService
{
    Task<NewProductAdmin?> ProductToWork(Guid userId, int productId);
    Task<bool> FinishModeration(FinishModerationRequest request);
    Task CreatePayout(CreatePayoutRequest request, Guid adminId);
    Task CreateIncomingTransfer(CreateIncomingTransferRequest request, Guid adminId);
    Task CreateBonus(CreateBonusRequest request, Guid adminId);
}
