using System.Text.Json;
using System.Text.Json.Serialization;
using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Constants;

public static class BlConstants
{
    public const string ProductCatIndex = "idx:products:{0}";
    public const string ProductIndex = "idx:products";
    public const string ProductSearchIndex = "idx:products:search";
    public const string CategorySearchIndexIndex = "idx:menucategies:search";

    public const Currency DefaultCurrency = Currency.USD;
    public const string CurrencyFormat = "0.00";
    public const long UserSpaceVolume = 1L * 1024 * 1024 * 1024; // 1 GB storage limit per user

    public static readonly JsonSerializerOptions RedisJsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static readonly JsonSerializerOptions BcJsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static readonly Dictionary<string, string> DmpEmails = new()
    {
        ["noreply"] = "noreply@filezon.com",
        ["store"] = "store@filezon.com"
    };

    public const string DefaultLanguage = "en";
    public static readonly List<string> SupportedLanguages = ["en", "ru"];
    public static readonly List<string> SupportedBcCryptos = ["btc", "ltc", "trx", "bch", "xmr", "eth", "bnb", "matic"];

    public const string LogoPath = "static/logo_color.png";
}
