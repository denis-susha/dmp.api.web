namespace DMP.BL.Services;

public interface IDownloadOrderService
{
    Task<string?> DownloadProduct(Guid userId, int orderId, int orderLineId);
}
