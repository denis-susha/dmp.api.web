using DMP.BL.Models.Auth;
using DMP.BL.Models.User;
using DMP.DataAccess.Models;

namespace DMP.BL.Services.User;

public interface IUserService
{
    Task<UserInfo?> GetUser(Guid userId);
    UserInfo GetUserInfo(UserDAL user);
    Task<ClientDynamicInfo> GetClientDynamicInfo(Guid userId);
    Task<UserDynamicInfo> GetUserDynamicInfo(Guid userId);
    Task<UserWelcomeInfo> GetWelcomeInfoSeller(Guid userId);
    Task<bool> CompleteTutorial(Guid userId);
    Task<bool> BecomeSeller(Guid userId);
    Task<UserSettings> GetUserSettings(Guid userId);
    Task<bool> UpdateUserProfileSettings(Guid userId, UserProfileSettings request);
    Task DeleteAccount(Guid userId, string refreshToken);
    Task CreateBillingAccounts(Guid userId);
}
