using DMP.DataAccess.Models.Enumerations;

namespace DMP.DataAccess.BillingModels;

public class CurrencySettingDAL
{
    public Currency CurrencyId { get; set; }
    public string CurrencyCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public byte? Divisibility { get; set; }
    public bool Crypto { get; set; }
    public string? Symbol { get; set; }
}
