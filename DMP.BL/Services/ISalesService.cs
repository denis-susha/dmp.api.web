using DMP.BL.Models;
using DMP.BL.Models.Sales;

namespace DMP.BL.Services;

public interface ISalesService
{
    Task<GetSalesResponse> GetSalesList(Guid userId, TableQuery request);
    Task<SalesViewHeader?> GetSaleView(Guid userId, int salesHeaderId);
}
