using DMP.BL.Constants;
using DMP.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMP.BL.Services;

public class TranslationService(ILogger<TranslationService> logger, DmpDbContext dbContext) : ITranslationService
{
    public async Task<string?> GetTranslation(string key, string lng = "en") =>
        await dbContext.Translations
            .Where(t => t.Key == key.ToLowerInvariant())
            .AsNoTracking()
            .Select(t => t.Translations[lng])
            .FirstOrDefaultAsync();

    public async Task<string> GetMenuTranslation(bool isCategory, int id, string name, string lng = "en")
    {
        var translationKey = TranslationKeys.GetMenuKey(isCategory, id, name);
        var translation = await GetTranslation(translationKey, lng);

        if (translation is null)
        {
            logger.LogWarning("Translation is missed for key: {TranslationKey}", translationKey);
            translation = name;
        }

        return translation;
    }
}
