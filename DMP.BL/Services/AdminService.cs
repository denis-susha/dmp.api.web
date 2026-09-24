using DMP.BL.Constants;
using DMP.BL.Models;
using DMP.BL.Models.Admin;
using DMP.BL.Models.Mail;
using DMP.BL.Models.ProductCreation;
using DMP.DataAccess;
using DMP.DataAccess.Models;
using DMP.DataAccess.Models.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMP.BL.Services;

public class AdminService(
    ILogger<AdminService> logger,
    IDbContextFactory<DmpDbContext> dmpContextFactory,
    IProductService productService,
    IMailService mailService,
    IApplicationSettingsService applicationSettingsService) : IAdminService
{
    public async Task<NewProductAdmin?> ProductToWork(Guid userId, int productId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var productDal = await context.Products
            .Include(p => p.ProductCreation)
            .Include(p => p.ProductImages!.Where(pi => pi.Attached))
            .Include(p => p.ProductFeatures)
            .Include(p => p.ProductFiles!.Where(pi => pi.IsUploaded && pi.Attached))
            .FirstOrDefaultAsync(p => p.ProductId == productId && (p.Status == ProductStatus.Built || p.Status == ProductStatus.Moderated));

        if (productDal is null)
        {
            return null;
        }

        if (productDal.Status != ProductStatus.Moderated)
        {
            productDal.Status = ProductStatus.Moderated;
            await context.SaveChangesAsync();

            logger.LogInformation("Admin {AdminId} changed product {ProductId} status to {Status}", userId, productId, productDal.Status);
        }

        var result = new NewProductAdmin
        {
            ProductId = productDal.ProductId,
            MenuCategoryId = productDal.ProductCreation!.UseCustomCategory ? null : productDal.MenuCategoryId,
            UseCustomCategory = productDal.ProductCreation.UseCustomCategory,
            CustomCategory = productDal.ProductCreation.UseCustomCategory
                ? productDal.ProductCreation.CustomCategory
                : null,
            UserFeatures = new UserFeature
            {
                Name = productDal.UserFeature!.Name,
                Description = productDal.UserFeature.Description
            },
            Features = productDal.ProductFeatures?.ToDictionary(pf => pf.FeatureId, pf => pf.Value),
            Price = productDal.Price,
            ProductImages = productDal.ProductImages?.Select(pi => new NewProductImage
            {
                Hash = pi.Hash,
                ImgUrl = pi.Images.Original,
                IsCover = pi.IsCover
            }),
            Slug = productDal.Slug,
            SellerId = productDal.SellerId,
            Status = productDal.Status,
            Lines = productDal.IsLines ? productDal.ProductCreation.Lines : null,
            IsLines = productDal.IsLines,
            Quantity = productDal.Quantity,
        };

        if (productDal.ProductFiles is not null)
        {
            result.Files = productDal.ProductFiles.Select(productFile => new GetProductFilesResponse
            {
                FileName = productFile.FileName,
                FileSize = productFile.FileSize,
                ProductFileId = productFile.ProductFileId
            }).ToList();
        }

        return result;
    }

    public async Task<bool> FinishModeration(FinishModerationRequest request)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var productDal = await context.Products
            .Include(p => p.ProductCreation)
            .Include(p => p.ProductFiles!.Where(pi => pi.IsUploaded && pi.Attached))
            .FirstOrDefaultAsync(p => p.ProductId == request.ProductId && p.Status == ProductStatus.Moderated);

        if (productDal is null)
        {
            return false;
        }

        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == productDal.SellerId);

        if (user is null)
        {
            return false;
        }

        var mailModel = new FinishModeration
        {
            LogoUrl = $"{applicationSettingsService.MinioSettings.S3PublicEndpoint}/{BlConstants.LogoPath}",
            Year = DateTime.UtcNow.Year.ToString(),
            ProductId = productDal.ProductId.ToString(),
            ProductName = productDal.UserFeature!.Name,
            UserName = user.Name
        };

        var categoryChanged = request.NewMenuCategoryId.HasValue &&
            (productDal.ProductCreation!.UseCustomCategory || productDal.MenuCategoryId != request.NewMenuCategoryId);

        if (categoryChanged || request.Status == FinishModerationStatus.NeedsImprovement)
        {
            if (categoryChanged)
            {
                // A new category invalidates the category-specific features, so the seller has to fill them in again.
                productDal.MenuCategoryId = request.NewMenuCategoryId!.Value;
                context.ProductFeatures.RemoveRange(context.ProductFeatures.Where(pf => pf.ProductId == request.ProductId));
            }

            productDal.Status = ProductStatus.New;

            productDal.ProductCreation!.MessageForSeller = request.MessageForSeller;
            productDal.ProductCreation.CustomCategory = null;
            productDal.ProductCreation.UseCustomCategory = false;

            context.ProductCreations.Update(productDal.ProductCreation);

            await context.SaveChangesAsync();

            mailModel.ProductLink = $"{applicationSettingsService.DmpHostsSettings.Seller}/products/view?id={productDal.ProductId}";
            mailModel.MessageForSeller = request.MessageForSeller;

            await mailService.CreateFinishModerationMail(mailModel, user.Email, user.Language, FinishModerationStatus.NeedsImprovement);

            return true;
        }

        // FinishModerationStatus is Ready
        if (!productDal.IsLines)
        {
            if (string.IsNullOrEmpty(request.FileName) || string.IsNullOrEmpty(request.FilePath) || request.FileSize < 1)
            {
                return false;
            }

            foreach (var productDalProductFile in productDal.ProductFiles!)
            {
                productDalProductFile.Attached = false;
                context.ProductFiles.Update(productDalProductFile);
            }

            context.ProductFiles.Add(new ProductFileDAL
            {
                ProductId = request.ProductId,
                UserId = productDal.SellerId,
                FileSize = request.FileSize!.Value,
                FileName = request.FileName,
                StoragePath = request.FilePath,
                IsUploaded = true,
                Attached = true
            });
        }
        else if (productDal.ProductCreation!.PreviousStatus != ProductStatus.PausedByUser)
        {
            foreach (var productCreationLine in productDal.ProductCreation.Lines!)
            {
                context.ProductLines.Add(new ProductLineDAL
                {
                    ProductId = productDal.ProductId,
                    Value = productCreationLine,
                    IsSold = false
                });
            }
        }

        productDal.ImgLinks = await context.ProductImages
            .Where(pi => pi.ProductId == productDal.ProductId && pi.Attached)
            .OrderByDescending(pi => pi.IsCover)
            .Select(pi => pi.Name)
            .ToArrayAsync();
        productDal.Status = ProductStatus.Ready;

        context.ProductCreations.Remove(productDal.ProductCreation!);

        await context.SaveChangesAsync();

        await productService.UpdateCacheProducts([productDal.ProductId]);

        mailModel.ProductLink = $"{applicationSettingsService.DmpHostsSettings.Client}/product/{productDal.Slug}-{productDal.ProductId}";

        await mailService.CreateFinishModerationMail(mailModel, user.Email, user.Language, FinishModerationStatus.Ready);

        return true;
    }

    public async Task CreatePayout(CreatePayoutRequest request, Guid adminId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();

        await EnsureAdmin(context, adminId);

        var payout = await context.Payouts.FirstAsync(p => p.PayoutId == request.PayoutId && p.Status == PayoutStatus.New);

        var user = await context.Users.FirstOrDefaultAsync(u => u.UserId == payout.UserId && u.Status == UserStatus.Active);

        if (user is null || !user.Flags.HasFlag(UserFlags.IsSeller))
        {
            throw new Exception("The user is not a seller or doesn't exist.");
        }

        payout.Amount = request.Amount;
        payout.NetworkFee = request.NetworkFee;
        payout.AdminId = adminId;
        payout.NetworkTransactionId = request.NetworkTransactionId;

        context.Payouts.Update(payout);
        context.TransactionWorkerTasks.Add(new TransactionWorkerTaskDAL
        {
            InvoiceId = payout.PayoutId,
            Status = TransactionWorkerTaskStatus.New,
            OrderId = -1,
            Type = TransactionWorkerTaskType.Payout,
            UserId = adminId
        });

        await context.SaveChangesAsync();
    }

    public async Task CreateIncomingTransfer(CreateIncomingTransferRequest request, Guid adminId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();

        await EnsureAdmin(context, adminId);

        context.TransactionWorkerTasks.Add(new TransactionWorkerTaskDAL
        {
            InvoiceId = request.InvoiceId,
            Status = TransactionWorkerTaskStatus.New,
            OrderId = -1,
            Type = TransactionWorkerTaskType.IncomingTransfer,
            UserId = adminId
        });
        await context.SaveChangesAsync();
    }

    public async Task CreateBonus(CreateBonusRequest request, Guid adminId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();

        await EnsureAdmin(context, adminId);

        var newFundsTransfer = new FundsTransferDAL
        {
            UserId = adminId,
            TargetUserId = request.TargetUserId,
            Amount = request.Amount,
            Cryptocurrency = request.Cryptocurrency,
            Comment = request.Comment,
            Status = FundsTransferStatus.New
        };

        context.FundsTransfers.Add(newFundsTransfer);
        await context.SaveChangesAsync();

        var user = await context.Users.FirstOrDefaultAsync(u => u.UserId == request.TargetUserId && u.Status == UserStatus.Active);

        if (user is null || !user.Flags.HasFlag(UserFlags.IsSeller))
        {
            throw new Exception("The user is not a seller or doesn't exist.");
        }

        context.TransactionWorkerTasks.Add(new TransactionWorkerTaskDAL
        {
            // The invoice id is irrelevant for bonus tasks.
            InvoiceId = Guid.NewGuid().ToString(),
            Status = TransactionWorkerTaskStatus.New,
            OrderId = newFundsTransfer.FundsTransferId,
            Type = TransactionWorkerTaskType.Bonus,
            UserId = adminId
        });

        await context.SaveChangesAsync();
    }

    private static async Task EnsureAdmin(DmpDbContext context, Guid adminId)
    {
        var admin = await context.Users.FirstAsync(u => u.UserId == adminId && u.Status == UserStatus.Active);

        if (!admin.Flags.HasFlag(UserFlags.IsAdmin))
        {
            throw new Exception("The user is not admin. The operation is forbidden.");
        }
    }
}
