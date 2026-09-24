using System.Linq.Dynamic.Core;
using DMP.BL.Models;
using DMP.BL.Models.Enumerations;
using DMP.BL.Models.ProductCreation;
using DMP.BL.Models.Sales;
using DMP.DataAccess;
using DMP.DataAccess.Models.Sale;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMP.BL.Services;

public class SalesService(ILogger<SalesService> logger, IDbContextFactory<DmpDbContext> dmpContextFactory) : ISalesService
{
    public async Task<GetSalesResponse> GetSalesList(Guid userId, TableQuery request)
    {
        var pageSize = request.Pagination.PageSize ?? 10;

        var result = new GetSalesResponse();

        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var salesQuery = context.SalesHeaders.AsNoTracking()
            .Where(p => p.SellerId == userId);

        result.TotalCount = await salesQuery.CountAsync();

        if (request.Sorting is { Count: > 0 })
        {
            var orderByClauses = new List<string>();

            foreach (var sortModel in request.Sorting)
            {
                if (Enum.TryParse(sortModel.Field, true, out SaleSortFields saleSort))
                {
                    Enum.TryParse(sortModel.Sort, true, out SortDirection sortDirection);
                    orderByClauses.Add(sortDirection == SortDirection.asc ? saleSort.ToString() : $"{saleSort} {sortDirection}");
                }
                else
                {
                    logger.LogError("Cannot parse sorting field {Field} for model {Model}", sortModel.Field, nameof(SalesHeaderDAL));
                }
            }

            orderByClauses.Add("SalesHeaderId desc");

            salesQuery = salesQuery.OrderBy(string.Join(", ", orderByClauses));
        }
        else
        {
            salesQuery = salesQuery.OrderByDescending(p => p.SalesHeaderId);
        }

        var salesDal = await salesQuery
            .Skip(request.Pagination.Page * pageSize)
            .Take(pageSize)
            .ToListAsync();

        result.Sales = salesDal.Select(s => new Sale
        {
            SalesHeaderId = s.SalesHeaderId,
            Amount = s.Amount,
            Cryptocurrency = s.Cryptocurrency,
            CreatedAt = s.CreatedAt,
        }).ToList();

        return result;
    }

    public async Task<SalesViewHeader?> GetSaleView(Guid userId, int salesHeaderId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var salesHeader = await context.SalesHeaders
            .Where(p => p.SellerId == userId && p.SalesHeaderId == salesHeaderId)
            .Include(sh => sh.SalesLines!).ThenInclude(sl => sl.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (salesHeader is null)
        {
            return null;
        }

        return new SalesViewHeader
        {
            SalesHeaderId = salesHeader.SalesHeaderId,
            Amount = salesHeader.Amount,
            Cryptocurrency = salesHeader.Cryptocurrency,
            CreatedAt = salesHeader.CreatedAt,
            SalesViewLines = salesHeader.SalesLines!.Select(sl => new SalesViewLine
            {
                SalesLineId = sl.SalesLineId,
                Price = sl.Price,
                Currency = sl.Currency.ToString(),
                Quantity = sl.Quantity,
                TransactionId = sl.TransactionId,
                SaleViewProduct = new SalesViewProduct
                {
                    ProductId = sl.Product.ProductId,
                    Name = sl.Product.UserFeature!.Name,
                    Slug = sl.Product.Slug,
                    ImgLink = sl.Product.ImgLinks?.FirstOrDefault(),
                }
            }).ToList(),
        };
    }
}
