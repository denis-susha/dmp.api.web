using DMP.BL.Constants;
using DMP.BL.Models.Auth;
using DMP.BL.Models.Enumerations;
using DMP.BL.Models.User;
using DMP.DataAccess;
using DMP.DataAccess.BillingModels;
using DMP.DataAccess.BillingModels.Enumerations;
using DMP.DataAccess.Models;
using DMP.DataAccess.Models.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace DMP.BL.Services.User;

public class UserService(
    IDbContextFactory<DmpDbContext> dmpContextFactory,
    IDbContextFactory<BillingDbContext> billingContextFactory,
    IFinancesService financesService,
    IRedisService redisService,
    IApplicationSettingsService applicationSettingsService) : IUserService
{
    public async Task<UserInfo?> GetUser(Guid userId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

        if (user is null || user.Status != UserStatus.Active)
        {
            return null;
        }

        return GetUserInfo(user);
    }

    public UserInfo GetUserInfo(UserDAL user) => new()
    {
        IsClient = user.Flags.HasFlag(UserFlags.IsClient),
        Role = user.Flags.HasFlag(UserFlags.IsAdmin) ? UserRole.Admin : user.Flags.HasFlag(UserFlags.IsSeller) ? UserRole.Seller : UserRole.Client,
        HideSellerStart = user.Flags.HasFlag(UserFlags.HideSellerStart),
        Name = user.Name
    };

    public async Task<ClientDynamicInfo> GetClientDynamicInfo(Guid userId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();

        return new ClientDynamicInfo
        {
            CartCount = await context.CartItems.AsNoTracking().CountAsync(st => st.UserId == userId),
            PendingOrdersCount = await context.OrderHeaders
                .CountAsync(f => f.UserId == userId && (f.Status == OrderStatus.New || f.Status == OrderStatus.PaidPartial))
        };
    }

    public async Task<UserDynamicInfo> GetUserDynamicInfo(Guid userId)
    {
        var result = new UserDynamicInfo();

        // The DB counters and the balance (which calls Bitcart for rates) are fetched concurrently.
        var dbInfoTask = LoadDbInfo();
        var balanceTask = financesService.GetBalance(userId);

        await Task.WhenAll(dbInfoTask, balanceTask);

        result.Balance = await balanceTask;

        return result;

        async Task LoadDbInfo()
        {
            await using var context = await dmpContextFactory.CreateDbContextAsync();
            result.SupportTicketsCount = await context.SupportTickets.AsNoTracking()
                .CountAsync(st => st.UserId == userId && st.Status <= SupportTicketStatus.Completed);
            result.UsedSpace = await context.ProductFiles
                .Where(f => f.UserId == userId && f.IsUploaded)
                .SumAsync(f => f.FileSize);
        }
    }

    public async Task<UserWelcomeInfo> GetWelcomeInfoSeller(Guid userId)
    {
        var result = new UserWelcomeInfo();

        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var user = await context.Users.AsNoTracking().FirstAsync(u => u.UserId == userId);
        result.EmailIsConfirmed = user.Status != UserStatus.Unverified;

        var store = await context.Stores.AsNoTracking().FirstAsync(u => u.SellerId == userId);
        result.StoreIsConfigured = store.Flags.HasFlag(StoreFlags.DataUpdated);

        result.HasProduct = await context.Products.AnyAsync(p => p.SellerId == userId);

        result.DoneStepsCount = (result.EmailIsConfirmed ? 1 : 0)
            + (result.StoreIsConfigured ? 1 : 0)
            + (result.HasProduct ? 1 : 0);

        return result;
    }

    public async Task<bool> CompleteTutorial(Guid userId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var user = await context.Users.FirstAsync(u => u.UserId == userId);
        user.Flags |= UserFlags.HideSellerStart;

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> BecomeSeller(Guid userId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var user = await context.Users.FirstAsync(u => u.UserId == userId);
        if (user.Flags.HasFlag(UserFlags.IsSeller))
        {
            return false;
        }

        user.Flags |= UserFlags.IsSeller;

        context.Stores.Add(new StoreDAL
        {
            SellerId = user.UserId,
            Name = user.Name
        });

        await context.SaveChangesAsync();

        await CreateBillingAccounts(userId);

        return true;
    }

    public async Task<UserSettings> GetUserSettings(Guid userId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var user = await context.Users.AsNoTracking().FirstAsync(u => u.UserId == userId && u.Status == UserStatus.Active);

        return new UserSettings
        {
            UserProfileSettings = new UserProfileSettings
            {
                Nickname = user.Name
            },
            Email = user.Email,
        };
    }

    public async Task<bool> UpdateUserProfileSettings(Guid userId, UserProfileSettings request)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var user = await context.Users.FirstAsync(u => u.UserId == userId && u.Status == UserStatus.Active);

        user.Name = request.Nickname.Trim();
        await context.SaveChangesAsync();

        return true;
    }

    public async Task DeleteAccount(Guid userId, string refreshToken)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var user = await context.Users.FirstAsync(u => u.UserId == userId && u.Status == UserStatus.Active);

        await redisService.DeleteCacheValue(string.Format(CacheKeys.UserRefreshToken, userId, refreshToken));

        user.Status = UserStatus.Inactive;
        await context.SaveChangesAsync();
    }

    public async Task CreateBillingAccounts(Guid userId)
    {
        await using var billingContext = await billingContextFactory.CreateDbContextAsync();
        foreach (var cryptocurrency in applicationSettingsService.CryptocurrencySettings)
        {
            billingContext.Accounts.Add(new AccountDAL
            {
                UserId = userId,
                AccountType = AccountType.User,
                Cryptocurrency = cryptocurrency.CryptocurrencyId,
                AccountBalance = 0,
                ReservedBalance = 0,
                Status = AccountStatus.Active
            });
        }

        await billingContext.SaveChangesAsync();
    }
}
