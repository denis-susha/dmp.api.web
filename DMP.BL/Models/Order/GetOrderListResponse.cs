namespace DMP.BL.Models.Order;

public class GetOrderListResponse
{
    public List<GetOrderResponse> Orders { get; set; } = null!;
    public int TotalCount { get; set; }
}
