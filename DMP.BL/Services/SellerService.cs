using DMP.BL.Models.Dashboard;
using DMP.BL.Models.Enumerations;
using DMP.BL.Models.Seller;
using DMP.BL.Services.User;
using DMP.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace DMP.BL.Services;

public class SellerService(
    IDbContextFactory<DmpDbContext> dmpContextFactory,
    IUserService userService,
    IStoreService storeService) : ISellerService
{
    public async Task<GetSellerUserResponse> GetSellerUser(Guid userId)
    {
        var userInfo = await userService.GetUser(userId);
        var store = await storeService.GetStore(userId);

        return new GetSellerUserResponse
        {
            UserInfo = userInfo,
            Store = store
        };
    }

    public async Task<DashboardInfo> GetDashboardInfo(Guid userId, Period period)
    {
        var periodDays = (int)period;
        var periodStart = DateTimeOffset.UtcNow.AddDays(-periodDays);

        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var salesHeadersDal = await context.SalesHeaders.AsNoTracking()
            .Where(sh => sh.SellerId == userId && sh.CreatedAt >= periodStart)
            .Include(sh => sh.SalesLines)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var startDate = today.AddDays(-periodDays);

        var stats = Enumerable.Range(0, (today - startDate).Days + 1)
            .Select(i => startDate.AddDays(i))
            .Select(day =>
            {
                var nextDay = day.AddDays(1);
                var sales = salesHeadersDal.Where(sh => sh.CreatedAt >= day && sh.CreatedAt < nextDay).ToList();

                return new DateSaleStat
                {
                    Date = day,
                    Count = sales.Sum(s => s.SalesLines?.Count ?? 0),
                    Amount = sales.Sum(s => s.SalesLines?.Sum(sl => sl.Price) ?? 0),
                };
            })
            .ToList();

        var topProducts = await context.GetTopSellProducts(userId);

        return new DashboardInfo
        {
            DateSaleStats = stats,
            TopSellProducts = topProducts.Select(t => new TopSellProduct
            {
                ProductId = t.ProductId,
                Name = t.Name,
                TotalSold = t.SalesQuantity,
                Slug = t.Slug,
                ImgLink = t.ImgLink,
                Price = t.Price,
            }).ToList()
        };
    }
}
