using DMP.DataAccess;
using DMP.DataAccess.Models.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace DMP.BL.Services;

public class DownloadOrderService(
    IDbContextFactory<DmpDbContext> dmpContextFactory,
    IProductFileStorageService productFileStorageService) : IDownloadOrderService
{
    private const int PresignedUrlExpirySeconds = 60 * 60;

    public async Task<string?> DownloadProduct(Guid userId, int orderId, int orderLineId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var orderExists = await context.OrderHeaders.AsNoTracking().AnyAsync(oh =>
            oh.OrderId == orderId && oh.UserId == userId &&
            (oh.Status == OrderStatus.Complete || oh.Status == OrderStatus.PaidOver));

        if (!orderExists)
        {
            return null;
        }

        var orderLine = await context.OrderLines.AsNoTracking()
            .FirstOrDefaultAsync(ol => ol.OrderLineId == orderLineId && ol.OrderId == orderId);

        if (orderLine is null)
        {
            return null;
        }

        var productDal = await context.Products.AsNoTracking()
            .Include(p => p.ProductFiles!.Where(pf => pf.Attached && pf.IsUploaded))
            .FirstOrDefaultAsync(p => p.ProductId == orderLine.ProductId && p.Status == ProductStatus.Ready);

        if (productDal is null || productDal.IsLines)
        {
            return null;
        }

        var productFile = productDal.ProductFiles!.FirstOrDefault();

        if (productFile is null)
        {
            return null;
        }

        return await productFileStorageService.GetFileDownloadPresignedUrl(productFile.StoragePath, PresignedUrlExpirySeconds);
    }
}
