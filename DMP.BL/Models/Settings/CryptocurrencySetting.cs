using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Models.Settings;

public class CryptocurrencySetting
{
    public Cryptocurrency CryptocurrencyId { get; set; }
    public string PlatformCode { get; set; } = null!;
    public string TokenCode { get; set; } = null!;
}
