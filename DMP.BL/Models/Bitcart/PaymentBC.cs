namespace DMP.BL.Models.Bitcart;

public class PaymentBC
{
    public string payment_url { get; set; } = null!;
    public string? node_id { get; set; }
    public string currency { get; set; } = null!;
    public string symbol { get; set; } = null!;
    public string amount { get; set; } = null!;
    public string? user_address { get; set; }
    public decimal recommended_fee { get; set; }
    public string label { get; set; } = null!;
    public string lookup_field { get; set; } = null!;
    public bool lightning { get; set; }
    public string created { get; set; } = null!;
    public int confirmations { get; set; }
    public string wallet_id { get; set; } = null!;
    public string? rhash { get; set; }
    public string hint { get; set; } = null!;
    public string rate { get; set; } = null!;
    public string id { get; set; } = null!;
    public string contract { get; set; } = null!;
    public object metadata { get; set; } = null!;
    public string? discount { get; set; }
    public string payment_address { get; set; } = null!;
    public int divisibility { get; set; }
    public string rate_str { get; set; } = null!;
    public string name { get; set; } = null!;
}
