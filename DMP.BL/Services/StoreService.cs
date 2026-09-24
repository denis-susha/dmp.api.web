using DMP.BL.Models.ProductCreation;
using DMP.BL.Models.Store;
using DMP.DataAccess;
using DMP.DataAccess.Models.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace DMP.BL.Services;

public class StoreService(IDbContextFactory<DmpDbContext> dmpContextFactory, IProductImagesService productImagesService)
    : IStoreService
{
    public async Task<Store?> GetStore(Guid userId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var storeDal = await context.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.SellerId == userId);

        if (storeDal is null)
        {
            return null;
        }

        return new Store
        {
            StoreId = storeDal.SellerId,
            Name = storeDal.Name,
            CoverPath = storeDal.CoverPath,
        };
    }

    public async Task<bool> UpdateStore(Guid userId, Store store)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var storeDal = await context.Stores.FirstOrDefaultAsync(s => s.SellerId == userId);

        if (storeDal is null)
        {
            return false;
        }

        storeDal.Name = store.Name;
        storeDal.Flags |= StoreFlags.DataUpdated;
        context.Stores.Update(storeDal);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<UploadProductImageResponse> UploadStoreImage(Guid userId, NewProductFile imageFile)
    {
        var result = new UploadProductImageResponse();

        await using var dbContext = await dmpContextFactory.CreateDbContextAsync();

        var storeDal = await dbContext.Stores.FirstOrDefaultAsync(s => s.SellerId == userId);

        if (storeDal is null)
        {
            result.Status = CreateOrUpdateProductStatus.InternalError;
            return result;
        }

        var (status, savedImage) = await productImagesService.SaveStoreImage(imageFile, userId);

        if (status != CreateOrUpdateProductStatus.Success)
        {
            result.Status = status;
            return result;
        }

        storeDal.CoverPath = savedImage!.FileName;

        dbContext.Stores.Update(storeDal);
        await dbContext.SaveChangesAsync();

        result.Status = CreateOrUpdateProductStatus.Success;

        return result;
    }
}
