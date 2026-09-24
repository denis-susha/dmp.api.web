using DMP.BL.Models.ApplicationSettings;
using DMP.BL.Models.AppSettings;
using DMP.BL.Models.Registration;
using DMP.BL.Models.Settings;

namespace DMP.BL.Services;

public interface IApplicationSettingsService
{
    JwtRegistrationSettings JwtRegistrationSettings { get; }
    MinioSettings MinioSettings { get; }
    DmpHostsSettings DmpHostsSettings { get; }
    CfSettings CfSettings { get; }
    List<CryptocurrencySetting> CryptocurrencySettings { get; }

    Task<BitcartSettings> GetBitcartSettings();
    Task<BitcartSettings> GenerateBitcartSettings();
}
