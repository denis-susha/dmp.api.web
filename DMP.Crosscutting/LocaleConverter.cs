namespace DMP.Crosscutting;

public static class LocaleConverter
{
    private const string DefaultLanguage = "en";
    private static readonly HashSet<string> SupportedLanguages = ["ru", "en"];

    /// <summary>
    /// Normalizes a locale (e.g. "en-US" -> "en") and falls back to the default language if it is not supported.
    /// </summary>
    public static string ConvertToSupportedLocale(string locale)
    {
        var normalizedLocale = NormalizeLocale(locale);
        return SupportedLanguages.Contains(normalizedLocale) ? normalizedLocale : DefaultLanguage;
    }

    private static string NormalizeLocale(string locale)
    {
        if (string.IsNullOrEmpty(locale))
        {
            return DefaultLanguage;
        }

        var parts = locale.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0].ToLowerInvariant() : DefaultLanguage;
    }
}
