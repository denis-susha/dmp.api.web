namespace DMP.BL.Models.Bitcart;

public class CreateInvoiceRequestBC
{
    // currency, price and store_id are required by Bitcart
    public string currency { get; set; } = null!;
    public string price { get; set; } = null!;
    public string store_id { get; set; } = null!;

    public object[] metadata { get; set; } = null!;
    public string? order_id { get; set; }
    public string? notification_url { get; set; }
    public string? redirect_url { get; set; }
    public string? buyer_email { get; set; }
    public string? promocode { get; set; }
    public string? shipping_address { get; set; }
    public string? notes { get; set; }
    public string[] products { get; set; } = null!;
    public int expiration { get; set; }
}
