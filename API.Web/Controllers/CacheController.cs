using Bogus;
using DMP.BL.Models;
using DMP.BL.Services;
using DMP.DataAccess;
using DMP.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Web.Controllers;

/// <summary>
/// Admin-only maintenance endpoints: rebuilds the Redis caches and generates test products.
/// </summary>
[Authorize(Policy = "RequireAdmin"), ApiController]
[Route("[controller]")]
public class CacheController(
    IRedisService redisService,
    IDbContextFactory<DmpDbContext> dmpContextFactory,
    IMenuService menuService,
    IProductImagesService productImagesService,
    ICacheManagerService cacheManagerService,
    IHttpClientFactory httpClientFactory,
    ILogger<CacheController> logger)
{
    private const string TestProductImageUrl = "https://ir-3.ozone.ru/s3/multimedia-1-s/wc1000/7071969880.jpg";
    private const int TestProductsPerCategory = 500;
    private static readonly Guid TestSellerId = new("0d3444c9-1dff-44ce-930e-708f77fa963a");

    [HttpGet]
    [Route("ReloadAll")]
    public async Task ReloadAll()
    {
        try
        {
            await cacheManagerService.ReloadAll();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reload all caches");
            throw;
        }
    }

    [HttpGet]
    [Route("ReloadAllProducts")]
    public async Task AddAllProducts()
    {
        try
        {
            await cacheManagerService.ReloadAllProducts();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reload products cache");
            throw;
        }
    }

    [HttpGet]
    [Route("ReloadMenus")]
    public async Task ReloadMenus()
    {
        try
        {
            await cacheManagerService.ReloadMenus();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reload menus cache");
            throw;
        }
    }

    [HttpGet]
    [Route("generateproducts")]
    public async Task GenerateProducts()
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var cats = await context.MenuCategories
            .Include(i => i.Children)
            .AsNoTracking()
            .ToListAsync();

        await GenerateTestProducts(cats);
    }

    [HttpGet]
    [Route("ReloadSearchCategories")]
    public async Task ReloadSearchCategories()
    {
        try
        {
            await cacheManagerService.CacheCategories();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reload search categories cache");
            throw;
        }
    }

    [HttpGet]
    [Route("ReloadCategoryFeaturesForProductManager")]
    public async Task ReloadCategoryFeaturesForProductManager()
    {
        try
        {
            await cacheManagerService.ReloadCategoryFeaturesForProductManager();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reload category features cache for product manager");
            throw;
        }
    }

    [HttpGet]
    [Route("ReloadProductCategories")]
    public async Task ReloadProductCategories()
    {
        try
        {
            await cacheManagerService.ReloadProductCategories();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reload product categories cache");
            throw;
        }
    }

    /// <summary>
    /// Generates fake products for every leaf category and stores them in Redis. All products share one image.
    /// </summary>
    private async Task GenerateTestProducts(List<MenuCategoryDAL> cats)
    {
        var uri = new Uri(TestProductImageUrl);
        using var httpClient = httpClientFactory.CreateClient();

        var fileExtension = Path.GetExtension(uri.GetLeftPart(UriPartial.Path));
        var imageBytes = await httpClient.GetByteArrayAsync(uri);

        string[] imgLinks = [await productImagesService.SaveImage(imageBytes, "wc1000", 1, 1, fileExtension)];

        foreach (var cat in cats)
        {
            var isLeaf = cat.ParentId.HasValue && (cat.Children is null || !cat.Children.Any());
            if (!isLeaf)
            {
                continue;
            }

            var product = new Faker<Product>()
                .RuleFor(o => o.ProductId, f => f.IndexGlobal)
                .RuleFor(o => o.MenuCategoryId, _ => cat.MenuCategoryId.ToString())
                .RuleFor(o => o.UserFeatures,
                    f => new Faker<UserFeature>()
                        .RuleFor(u => u.Name, uf => uf.Lorem.Sentence(f.Random.Int(1, 5)))
                        .RuleFor(u => u.Description, uf => uf.Lorem.Text())
                        .RuleFor(u => u.Features, _ => new Dictionary<string, string>
                        {
                            { "testprop", "testval" }
                        })
                )
                .RuleFor(f => f.Slug, f => f.Lorem.Slug())
                .RuleFor(f => f.SellerId, TestSellerId)
                .RuleFor(f => f.Unlimited, f => f.PickRandom(true, false, true))
                .RuleFor(p => p.Quantity, (f, p) => p.Unlimited ? 0 : f.Random.Int(1, 5000))
                .RuleFor(o => o.Price, f => f.Finance.Amount(0.01m, 2))
                .RuleFor(f => f.ImgLinks, _ => imgLinks);

            var rootCatId = menuService.FindRootCategory(cats, cat.MenuCategoryId);
            redisService.SetProducts(product.Generate(TestProductsPerCategory), rootCatId);

            redisService.EnsureProductCatIndex(rootCatId);
        }

        redisService.EnsureProductIndex();
    }
}
