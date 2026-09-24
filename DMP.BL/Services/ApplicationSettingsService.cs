using System.Text.Json;
using DMP.BL.Constants;
using DMP.BL.Models.ApplicationSettings;
using DMP.BL.Models.AppSettings;
using DMP.BL.Models.Registration;
using DMP.BL.Models.Settings;
using DMP.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DMP.BL.Services;

public class ApplicationSettingsService : IApplicationSettingsService
{
    private readonly IDbContextFactory<DmpDbContext> _dmpContextFactory;
    private readonly IRedisService _redisService;

    public JwtRegistrationSettings JwtRegistrationSettings { get; }
    public MinioSettings MinioSettings { get; }
    public DmpHostsSettings DmpHostsSettings { get; }
    public CfSettings CfSettings { get; }
    public List<CryptocurrencySetting> CryptocurrencySettings { get; }

    public ApplicationSettingsService(
        IDbContextFactory<DmpDbContext> dmpContextFactory,
        IRedisService redisService,
        IOptions<JwtRegistrationSettings> jwtRegistrationSettings,
        IOptions<MinioSettings> s3Options,
        IOptions<DmpHostsSettings> dmpHostsOptions,
        IOptions<CfSettings> cfSettings)
    {
        _dmpContextFactory = dmpContextFactory;
        _redisService = redisService;
        JwtRegistrationSettings = jwtRegistrationSettings.Value;
        MinioSettings = s3Options.Value;
        DmpHostsSettings = dmpHostsOptions.Value;
        CfSettings = cfSettings.Value;

        // Loaded eagerly and synchronously: this is a singleton and the crypto settings are static reference data.
        using var context = _dmpContextFactory.CreateDbContext();
        CryptocurrencySettings = context.CryptocurrencySettings.AsNoTracking().Select(cs => new CryptocurrencySetting
        {
            CryptocurrencyId = cs.CryptocurrencyId,
            PlatformCode = cs.PlatformCode,
            TokenCode = cs.TokenCode,
        }).ToList();
    }

    public async Task<BitcartSettings> GetBitcartSettings()
    {
        var cachedSettings = await _redisService.GetCacheValue<BitcartSettings>(CacheKeys.ApplicationSettingsBitcart);

        if (cachedSettings is not null)
        {
            return cachedSettings;
        }

        var bitcartSettings = await GenerateBitcartSettings();

        await _redisService.SetCacheValue(CacheKeys.ApplicationSettingsBitcart, bitcartSettings);

        return bitcartSettings;
    }

    public async Task<BitcartSettings> GenerateBitcartSettings()
    {
        await using var context = await _dmpContextFactory.CreateDbContextAsync();
        var setting = await context.ApplicationSettings
            .AsNoTracking()
            .FirstAsync(s => s.Key == CacheKeys.ApplicationSettingsBitcart);

        return JsonSerializer.Deserialize<BitcartSettings>(setting.Value, BlConstants.JsonSerializerOptions)!;
    }
}
