namespace DMP.BL.Models.Order;

public class GetOrderLineDataResponse
{
    public bool IsLine { get; set; }
    public string? LineData { get; set; }
    public long? FileSize { get; set; }
    public string? FileName { get; set; }
    /// <summary>Order line id.</summary>
    public int? FileLineIdentificator { get; set; }
}
