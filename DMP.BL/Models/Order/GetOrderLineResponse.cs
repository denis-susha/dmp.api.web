namespace DMP.BL.Models.Order;

public class GetOrderLineResponse
{
    public string Slug { get; set; } = null!;
    public string? Cover { get; set; }
    public decimal Price { get; set; }
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public int ProductId { get; set; }
    public string StoreName { get; set; } = null!;
    public string FeaturesValues { get; set; } = null!;
    public GetOrderLineDataResponse? Data { get; set; }
}
