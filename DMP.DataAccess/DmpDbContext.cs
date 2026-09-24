using System.Text.Json;
using System.Text.Json.Serialization;
using DMP.DataAccess.Models;
using DMP.DataAccess.Models.Order;
using DMP.DataAccess.Models.Sale;
using Microsoft.EntityFrameworkCore;

namespace DMP.DataAccess;

public class DmpDbContext(DbContextOptions<DmpDbContext> options) : DbContext(options)
{
    private static readonly JsonSerializerOptions JsonbWriteOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions JsonbReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DbSet<MenuDAL> Menus { get; set; }
    public DbSet<MenuCategoryDAL> MenuCategories { get; set; }
    public DbSet<TranslationDAL> Translations { get; set; }
    public DbSet<PageContentDAL> PageContents { get; set; }
    public DbSet<ProductDAL> Products { get; set; }
    public DbSet<ProductCreationDAL> ProductCreations { get; set; }
    public DbSet<ProductImageDAL> ProductImages { get; set; }
    public DbSet<FeatureDAL> Features { get; set; }
    public DbSet<ProductFeatureDAL> ProductFeatures { get; set; }
    public DbSet<MailDAL> Mails { get; set; }
    public DbSet<UserDAL> Users { get; set; }
    public DbSet<EmailConfirmationDAL> EmailConfirmations { get; set; }
    public DbSet<ApplicationSettingsDAL> ApplicationSettings { get; set; }
    public DbSet<OrderHeaderDAL> OrderHeaders { get; set; }
    public DbSet<OrderLineDAL> OrderLines { get; set; }
    public DbSet<CartItemDAL> CartItems { get; set; }
    public DbSet<InvoiceWorkerTaskDAL> InvoiceWorkerTasks { get; set; }
    public DbSet<SalesHeaderDAL> SalesHeaders { get; set; }
    public DbSet<SalesLineDAL> SalesLines { get; set; }
    public DbSet<CategoryFeatureDAL> CategoryFeatures { get; set; }
    public DbSet<ProductFileDAL> ProductFiles { get; set; }
    public DbSet<ProductLineDAL> ProductLines { get; set; }
    public DbSet<EmailTemplateDAL> EmailTemplates { get; set; }
    public DbSet<CryptocurrencySettingDAL> CryptocurrencySettings { get; set; }
    public DbSet<PaymentMethodDAL> PaymentMethods { get; set; }
    public DbSet<SupportTicketDAL> SupportTickets { get; set; }
    public DbSet<SupportTicketMessageDAL> SupportTicketMessages { get; set; }
    public DbSet<StoreDAL> Stores { get; set; }
    public DbSet<JobProductCacheTaskDAL> JobProductCacheTasks { get; set; }
    public DbSet<PayoutDAL> Payouts { get; set; }
    public DbSet<TransactionWorkerTaskDAL> TransactionWorkerTasks { get; set; }
    public DbSet<BlogPostDAL> BlogPosts { get; set; }
    public DbSet<FundsTransferDAL> FundsTransfers { get; set; }

    /// <summary>
    /// Returns the id of the category and the ids of all its descendants, or <c>null</c> if the category does not exist.
    /// </summary>
    public List<int>? GetAllCategoryIds(int categoryId)
    {
        if (!MenuCategories.Any(mc => mc.MenuCategoryId == categoryId))
        {
            return null;
        }

        List<int> categoryIds = [categoryId];

        var childCategoryIds = MenuCategories
            .Where(c => c.ParentId == categoryId)
            .Select(c => c.MenuCategoryId)
            .ToList();

        foreach (var childCategoryId in childCategoryIds)
        {
            var descendantIds = GetAllCategoryIds(childCategoryId);
            if (descendantIds is not null)
            {
                categoryIds.AddRange(descendantIds);
            }
        }

        return categoryIds;
    }

    public Task<List<TopSellProductDAL>> GetTopSellProducts(Guid sellerId) =>
        Database
            .SqlQuery<TopSellProductDAL>($"SELECT * FROM get_top_sales_by_seller({sellerId})")
            .ToListAsync();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MenuDAL>(entity =>
        {
            entity.ToTable("Menu");
            entity.HasKey(p => p.MenuId);

            entity.Property(p => p.Description)
                .HasColumnName("Description")
                .HasMaxLength(100);
        });

        modelBuilder.Entity<MenuCategoryDAL>(entity =>
        {
            entity.ToTable("MenuCategory");
            entity.HasKey(p => p.MenuCategoryId);

            entity.Property(e => e.MenuId)
                .HasColumnName("MenuId");

            entity.Property(p => p.Name)
                .IsRequired()
                .HasColumnName("Name")
                .HasMaxLength(100);

            entity.Property(p => p.ParentId)
                .HasColumnName("ParentId");

            entity.Property(p => p.Order)
                .HasColumnName("Order");

            entity.Property(p => p.Slug)
                .IsRequired()
                .HasColumnName("Slug")
                .HasMaxLength(5000);

            entity.HasOne(p => p.Menu)
                .WithMany(m => m.Categories)
                .HasForeignKey(k => k.MenuId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Menu_MenuId_MenuId");

            entity.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_MenuCategory_ParentId");
        });

        modelBuilder.Entity<TranslationDAL>(entity =>
        {
            entity.ToTable("Translation");
            entity.HasKey(p => p.Key);

            entity.Property(e => e.Key)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(t => t.Translations)
                .HasColumnType("jsonb");
        });

        modelBuilder.Entity<ProductDAL>(entity =>
        {
            entity.ToTable("Product");
            entity.HasKey(p => p.ProductId);
            entity.Property(e => e.ProductId).ValueGeneratedOnAdd();
            entity.Property(e => e.MenuCategoryId)
                .IsRequired();

            entity.Property(t => t.UserFeature)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonbWriteOptions),
                    v => JsonSerializer.Deserialize<UserFeatureDAL>(v, JsonbReadOptions)
                )
                .HasColumnType("jsonb");

            entity.Property(e => e.Slug)
                .IsRequired()
                .HasMaxLength(5000);

            entity.Property(e => e.SellerId).IsRequired();
            entity.Property(e => e.Price).IsRequired();
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.Unlimited).IsRequired();
            entity.Property(e => e.ImgLinks);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();

            entity.HasOne(p => p.MenuCategory)
                .WithMany(m => m.Products)
                .HasForeignKey(k => k.MenuCategoryId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_MenuCategory_MenuCategoryId");

            entity.HasOne(cf => cf.ProductCreation)
                .WithOne(f => f.Product)
                .HasForeignKey<ProductCreationDAL>(p => p.ProductId)
                .HasConstraintName("FK_ProductCreation_Product");
        });

        modelBuilder.Entity<ProductCreationDAL>(entity =>
        {
            entity.ToTable("ProductCreation");
            entity.HasKey(p => p.ProductCreationId);
            entity.Property(e => e.ProductCreationId).ValueGeneratedOnAdd();
            entity.Property(e => e.ProductId);
            entity.Property(e => e.CustomCategory);
            entity.Property(e => e.UseCustomCategory).IsRequired();
            entity.Property(e => e.Lines);
            entity.Property(e => e.MessageForSeller);
            entity.Property(e => e.PreviousStatus);
        });

        modelBuilder.Entity<ProductImageDAL>(entity =>
        {
            entity.ToTable("ProductImage");
            entity.HasKey(p => new { p.ProductId, p.Hash });

            entity.Property(e => e.Hash).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Images).IsRequired()
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonbWriteOptions),
                    v => JsonSerializer.Deserialize<StoredImageDAL>(v, JsonbReadOptions)!
                )
                .HasColumnType("jsonb");
            entity.Property(e => e.IsCover).IsRequired();
            entity.Property(e => e.Attached).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(31);

            entity.HasOne(cf => cf.Product)
                .WithMany(f => f.ProductImages)
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProductImage_Product");
        });

        modelBuilder.Entity<FeatureDAL>(entity =>
        {
            entity.ToTable("Feature");
            entity.HasKey(p => p.FeatureId);

            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.CheckboxesValues);
            entity.Property(e => e.RangeMin);
            entity.Property(e => e.RangeMax);
            entity.Property(e => e.RangeFrom);
            entity.Property(e => e.RangeTo);
            entity.Property(e => e.MultipleChoice);
            entity.Property(e => e.RangeType);
        });

        modelBuilder.Entity<ProductFeatureDAL>(entity =>
        {
            entity.ToTable("ProductFeature");
            entity.HasKey(p => new { p.ProductId, p.FeatureId });

            entity.Property(e => e.Value);

            entity.HasOne(p => p.Product)
                .WithMany(i => i.ProductFeatures)
                .HasForeignKey(p => p.ProductId);

            entity.HasOne(p => p.Feature)
                .WithMany(f => f.ProductFeatures)
                .HasForeignKey(p => p.FeatureId);
        });

        modelBuilder.Entity<MailDAL>(entity =>
        {
            entity.ToTable("Mail");
            entity.HasKey(p => p.MailId);

            entity.Property(e => e.MailId).HasColumnName("MailId").IsRequired();
            entity.Property(e => e.To).HasColumnName("To").IsRequired().HasMaxLength(320);
            entity.Property(e => e.From).HasColumnName("From").IsRequired().HasMaxLength(320);
            entity.Property(e => e.Subject).HasColumnName("Subject").HasMaxLength(988);
            entity.Property(e => e.Copy).HasColumnName("Copy").HasMaxLength(1000);
            entity.Property(e => e.Body).HasColumnName("Body");
            entity.Property(e => e.Status).HasColumnName("Status").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql();
            entity.Property(e => e.Attempts).HasColumnName("Attempts").IsRequired();
            entity.Property(e => e.EmailTemplateId);
            entity.Property(t => t.Model);

            entity.HasOne(p => p.EmailTemplate)
                .WithMany(f => f.Mails)
                .HasForeignKey(p => p.EmailTemplateId);
        });

        modelBuilder.Entity<UserDAL>(entity =>
        {
            entity.ToTable("User");
            entity.HasKey(p => p.UserId);

            entity.Property(e => e.UserId).HasColumnName("UserId").IsRequired();
            entity.Property(e => e.Email).HasColumnName("Email").IsRequired().HasMaxLength(320);
            entity.Property(e => e.Password).HasColumnName("Password").IsRequired();
            entity.Property(e => e.Salt).HasColumnName("Salt").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();
            entity.Property(e => e.Status).HasColumnName("Status").HasDefaultValueSql();
            entity.Property(e => e.Flags).HasColumnName("Flags").IsRequired();
            entity.Property(e => e.Language).HasColumnName("Language").IsRequired();
            entity.Property(e => e.Name).HasColumnName("Name").IsRequired().HasMaxLength(200);

            entity.HasOne(cf => cf.Store)
                .WithOne(f => f.Seller)
                .HasForeignKey<StoreDAL>(p => p.SellerId)
                .HasConstraintName("FK_Store_SellerId_User_UserId");
        });

        modelBuilder.Entity<EmailConfirmationDAL>(entity =>
        {
            entity.ToTable("EmailConfirmation");
            entity.HasKey(p => p.EmailConfirmationId);

            entity.Property(e => e.EmailConfirmationId).HasColumnName("EmailConfirmationId").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("UserId").IsRequired();
            entity.Property(e => e.Token).HasColumnName("Token").IsRequired();
            entity.Property(e => e.Used).HasColumnName("Used").HasDefaultValueSql();
            entity.Property(e => e.SentAt).HasColumnName("SentAt").IsRequired();
            entity.Property(e => e.Type).HasColumnName("Type").IsRequired();
        });

        modelBuilder.Entity<ApplicationSettingsDAL>(entity =>
        {
            entity.ToTable("ApplicationSettings");
            entity.HasKey(p => p.Key);

            entity.Property(e => e.Key).HasColumnName("Key").IsRequired();
            entity.Property(e => e.Value).HasColumnName("Value").HasColumnType("jsonb");
        });

        modelBuilder.Entity<OrderHeaderDAL>(entity =>
        {
            entity.ToTable("OrderHeader");
            entity.HasKey(p => p.OrderId);
            entity.Property(e => e.OrderId).HasColumnName("OrderId").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("UserId").IsRequired();
            entity.Property(e => e.Amount).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();
            entity.Property(e => e.Status).HasColumnName("Status").IsRequired();
            entity.Property(e => e.ReferenceNumber).HasColumnName("ReferenceNumber").HasMaxLength(200);
            entity.Property(e => e.Currency).HasColumnName("Currency").IsRequired();
        });

        modelBuilder.Entity<OrderLineDAL>(entity =>
        {
            entity.ToTable("OrderLine");
            entity.HasKey(p => p.OrderLineId);
            entity.Property(p => p.OrderLineId).IsRequired();
            entity.Property(e => e.OrderId).HasColumnName("OrderId").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("ProductId").IsRequired();
            entity.Property(e => e.SellerId).HasColumnName("SellerId").IsRequired();
            entity.Property(e => e.Price).HasColumnName("Price").IsRequired();
            entity.Property(e => e.Quantity).HasColumnName("Quantity").IsRequired();
            entity.Property(e => e.Currency).HasColumnName("Currency").IsRequired();
            entity.Property(e => e.IsLine).HasColumnName("IsLine").IsRequired();
            entity.Property(e => e.ProductFileId).HasColumnName("ProductFileId");
            entity.Property(e => e.ProductLineId).HasColumnName("ProductLineId");

            entity.HasOne(p => p.OrderHeader)
                .WithMany(m => m.OrderLines)
                .HasForeignKey(k => k.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_OrderLine_OrderId_OrderHeader_OrderId");
        });

        modelBuilder.Entity<CartItemDAL>(entity =>
        {
            entity.ToTable("CartItem");
            entity.HasKey(p => new { p.UserId, p.ProductId });
            entity.Property(e => e.UserId).HasColumnName("UserId").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("ProductId").IsRequired();
            entity.Property(e => e.Quantity).HasColumnName("Quantity").IsRequired();
            entity.Property(e => e.Selected).HasColumnName("Selected").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();
        });

        modelBuilder.Entity<InvoiceWorkerTaskDAL>(entity =>
        {
            entity.ToTable("InvoiceWorkerTask");
            entity.HasKey(p => p.InvoiceId);
            entity.Property(e => e.Status).HasColumnName("Status").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();
            entity.Property(e => e.OrderId).HasColumnName("OrderId").IsRequired();
        });

        modelBuilder.Entity<SalesHeaderDAL>(entity =>
        {
            entity.ToTable("SalesHeader");
            entity.HasKey(p => p.SalesHeaderId);
            entity.Property(e => e.SellerId).HasColumnName("SellerId").IsRequired();
            entity.Property(e => e.Amount).HasColumnName("Amount").IsRequired();
            entity.Property(e => e.Cryptocurrency).HasColumnName("Cryptocurrency").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();
            entity.Property(e => e.OrderId).HasColumnName("OrderId").IsRequired();
        });

        modelBuilder.Entity<SalesLineDAL>(entity =>
        {
            entity.ToTable("SalesLine");
            entity.HasKey(p => p.SalesLineId);
            entity.Property(p => p.SalesHeaderId).HasColumnName("SalesHeaderId").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("ProductId").IsRequired();
            entity.Property(e => e.Price).HasColumnName("Price").IsRequired();
            entity.Property(e => e.Currency).HasColumnName("Currency").IsRequired();
            entity.Property(e => e.Quantity).HasColumnName("Quantity").IsRequired();
            entity.Property(e => e.TransactionId).HasColumnName("TransactionId").IsRequired().HasMaxLength(26);

            entity.HasOne(p => p.SalesHeader)
                .WithMany(m => m.SalesLines)
                .HasForeignKey(k => k.SalesHeaderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SalesLine_SalesHeaderId_SalesHeader_SalesHeaderId");

            entity.HasOne(p => p.Product)
                .WithMany(m => m.SalesLines)
                .HasForeignKey(k => k.ProductId)
                .HasConstraintName("FK_SalesLine_ProductId_Product_ProductId");
        });

        modelBuilder.Entity<CategoryFeatureDAL>(entity =>
        {
            entity.ToTable("CategoryFeature");
            entity.HasKey(p => new { p.CategoryId, p.FeatureId });

            entity.HasOne(cf => cf.Feature)
                .WithMany(f => f.CategoryFeatures)
                .HasForeignKey(cf => cf.FeatureId);

            entity.HasOne(cf => cf.MenuCategory)
                .WithMany(f => f.CategoryFeatures)
                .HasForeignKey(cf => cf.CategoryId);
        });

        modelBuilder.Entity<ProductFileDAL>(entity =>
        {
            entity.ToTable("ProductFile");
            entity.HasKey(p => p.ProductFileId);
            entity.Property(p => p.ProductId).IsRequired();
            entity.Property(p => p.UserId).IsRequired();
            entity.Property(p => p.FileSize).IsRequired();
            entity.Property(p => p.FileName).IsRequired();
            entity.Property(p => p.StoragePath).IsRequired();
            entity.Property(p => p.IsUploaded).IsRequired();

            entity.HasOne(cf => cf.Product)
                .WithMany(f => f.ProductFiles)
                .HasForeignKey(cf => cf.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProductFile_Product");
        });

        modelBuilder.Entity<ProductLineDAL>(entity =>
        {
            entity.ToTable("ProductLine");
            entity.HasKey(p => p.ProductLineId);
            entity.Property(p => p.ProductId).IsRequired();
            entity.Property(p => p.Value).IsRequired();
            entity.Property(p => p.IsSold).IsRequired();

            entity.HasOne(cf => cf.Product)
                .WithMany(f => f.ProductLines)
                .HasForeignKey(cf => cf.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ProductLines_Product");
        });

        modelBuilder.Entity<EmailTemplateDAL>(entity =>
        {
            entity.ToTable("EmailTemplate");
            entity.HasKey(p => p.EmailTemplateId);
            entity.Property(p => p.Name).IsRequired();
            entity.Property(p => p.Subject).IsRequired();
            entity.Property(p => p.Body).IsRequired();
            entity.Property(p => p.Language).IsRequired();
        });

        modelBuilder.Entity<CryptocurrencySettingDAL>(entity =>
        {
            entity.ToTable("CryptocurrencySetting");
            entity.HasKey(p => p.CryptocurrencyId);
            entity.Property(p => p.Code).IsRequired().HasMaxLength(15);
            entity.Property(p => p.PlatformCode).IsRequired().HasMaxLength(5);
            entity.Property(p => p.PlatformName).IsRequired();
            entity.Property(p => p.TokenCode).IsRequired().HasMaxLength(5);
            entity.Property(p => p.TokenName).IsRequired();
            entity.Property(p => p.Contract);
            entity.Property(p => p.Standart);
            entity.Property(p => p.Divisibility).IsRequired();
        });

        modelBuilder.Entity<PaymentMethodDAL>(entity =>
        {
            entity.ToTable("PaymentMethod");
            entity.HasKey(p => p.PaymentMethodId);
            entity.Property(p => p.OrderId).IsRequired();
            entity.Property(p => p.BcPaymentMethodId).IsRequired().HasMaxLength(32);
            entity.Property(p => p.UserId).IsRequired();
        });

        modelBuilder.Entity<SupportTicketDAL>(entity =>
        {
            entity.ToTable("SupportTicket");
            entity.HasKey(p => p.SupportTicketId);
            entity.Property(p => p.UserId).IsRequired();
            entity.Property(p => p.Type).IsRequired();
            entity.Property(p => p.Subject).IsRequired().HasMaxLength(250);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<SupportTicketMessageDAL>(entity =>
        {
            entity.ToTable("SupportTicketMessage");
            entity.HasKey(p => p.SupportTicketMessageId);
            entity.Property(p => p.SupportTicketId).IsRequired();
            entity.Property(p => p.SenderUserId).IsRequired();
            entity.Property(p => p.Message).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql();
            entity.Property(e => e.Type).IsRequired();

            entity.HasOne(p => p.SupportTicket)
                .WithMany(m => m.SupportTicketMessages)
                .HasForeignKey(k => k.SupportTicketId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_SupportTicketMessage_SupportTicketMessageId_SupportTicket_SupportTicketMessageId");
        });

        modelBuilder.Entity<StoreDAL>(entity =>
        {
            entity.ToTable("Store");
            entity.HasKey(p => p.SellerId);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(250);
            entity.Property(p => p.CoverPath).HasMaxLength(250);
            entity.Property(p => p.Flags).IsRequired();
        });

        modelBuilder.Entity<PageContentDAL>(entity =>
        {
            entity.ToTable("PageContent");
            entity.HasKey(p => p.PageContentId);
            entity.Property(e => e.Language).IsRequired();
            entity.Property(e => e.Key).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Content).HasColumnType("jsonb");
        });

        modelBuilder.Entity<JobProductCacheTaskDAL>(entity =>
        {
            entity.ToTable("JobProductCacheTask");
            entity.HasKey(p => p.JobProductCacheTaskId);
            entity.Property(p => p.ProductId).IsRequired();
            entity.Property(p => p.CreatedAt).HasDefaultValueSql();
        });

        modelBuilder.Entity<PayoutDAL>(entity =>
        {
            entity.ToTable("Payout");
            entity.HasKey(p => p.PayoutId);
            entity.Property(p => p.PayoutId).IsRequired().HasMaxLength(29); // "01-" prefix + UUID
            entity.Property(p => p.UserId).IsRequired();
            entity.Property(p => p.Amount).IsRequired();
            entity.Property(p => p.NetworkFee).IsRequired();
            entity.Property(p => p.Cryptocurrency).IsRequired();
            entity.Property(p => p.Status).IsRequired();
            entity.Property(p => p.CreatedAt).HasDefaultValueSql();
            entity.Property(p => p.AdminId);
            entity.Property(p => p.UserAmount).IsRequired();
            entity.Property(p => p.Address).IsRequired().HasMaxLength(104);
            entity.Property(p => p.NetworkTransactionId).HasMaxLength(300);
        });

        modelBuilder.Entity<TransactionWorkerTaskDAL>(entity =>
        {
            entity.ToTable("TransactionWorkerTask");
            entity.HasKey(p => p.InvoiceId);
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql();
            entity.Property(e => e.Status).HasColumnName("Status").IsRequired();
            entity.Property(e => e.OrderId).HasColumnName("OrderId").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasDefaultValueSql();
            entity.Property(e => e.Attempts).HasColumnName("Attempts").HasDefaultValueSql();
            entity.Property(e => e.SourceTransactionId).HasColumnName("SourceTransactionId");
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Comment);
        });

        modelBuilder.Entity<BlogPostDAL>(entity =>
        {
            entity.ToTable("BlogPost");
            entity.HasKey(p => p.BlogPostId);
            entity.Property(e => e.Slug).IsRequired();
            entity.Property(e => e.Language).IsRequired();
            entity.Property(e => e.CoverPath).IsRequired();
            entity.Property(e => e.PublishedAt).IsRequired();
            entity.Property(e => e.MinRead).IsRequired();
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.ShortContent).IsRequired();
            entity.Property(e => e.Content).IsRequired();

            entity.Property(t => t.Attributes)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonbWriteOptions),
                    v => JsonSerializer.Deserialize<BlogPostAttributesDAL>(v, JsonbReadOptions)!
                )
                .HasColumnType("jsonb")
                .IsRequired();
        });

        modelBuilder.Entity<FundsTransferDAL>(entity =>
        {
            entity.ToTable("FundsTransfer");
            entity.HasKey(p => p.FundsTransferId);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.TargetUserId).IsRequired();
            entity.Property(e => e.Amount).IsRequired();
            entity.Property(e => e.Cryptocurrency).IsRequired();
            entity.Property(e => e.Comment);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql();
        });

        base.OnModelCreating(modelBuilder);
    }
}
