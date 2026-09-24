namespace DMP.BL.Models.Bitcart;

public class InvoiceResponseBC
{
    public object metadata { get; set; } = null!;
    public string created { get; set; } = null!;
    public string price { get; set; } = null!;
    public string store_id { get; set; } = null!;
    public string currency { get; set; } = null!;
    public string? paid_currency { get; set; }
    public decimal? sent_amount { get; set; }
    public string order_id { get; set; } = null!;
    public string notification_url { get; set; } = null!;
    public string redirect_url { get; set; } = null!;
    public string? buyer_email { get; set; }
    public string promocode { get; set; } = null!;
    public string shipping_address { get; set; } = null!;
    public string notes { get; set; } = null!;
    public string? discount { get; set; }
    public string status { get; set; } = null!;
    public string exception_status { get; set; } = null!;
    public object products { get; set; } = null!;
    public string[] tx_hashes { get; set; } = null!;
    public int expiration { get; set; }
    public string id { get; set; } = null!;
    public string user_id { get; set; } = null!;
    public int time_left { get; set; }
    public int expiration_seconds { get; set; }
    public Dictionary<string, string>? product_names { get; set; }
    public string? paid_date { get; set; }
    public PaymentBC[] payments { get; set; } = null!;
    public string? refund_id { get; set; }
}
