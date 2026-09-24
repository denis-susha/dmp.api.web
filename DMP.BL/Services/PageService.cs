using System.Text.Json;
using DMP.BL.Constants;
using DMP.BL.Helpers;
using DMP.BL.Models.Page;
using DMP.DataAccess;
using DMP.DataAccess.Models.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace DMP.BL.Services;

public class PageService(IRedisService redisService, IDbContextFactory<DmpDbContext> dmpContextFactory) : IPageService
{
    private static readonly string[] AllowedPageTypes = ["modal"];
    private static readonly string[] AllowedElementTypes = ["selectLanguage"];

    public async Task<object?> GetJson(string path, string lng)
    {
        var pathArray = path.Split(".");
        if (pathArray.Length < 2 || !AllowedPageTypes.Contains(pathArray[0]) || !AllowedElementTypes.Contains(pathArray[1]))
        {
            return null;
        }

        var key = string.Join(".", pathArray);
        return pathArray[0] switch
        {
            "modal" => await GetModalPage(pathArray[1], key, lng),
            _ => null
        };
    }

    private async Task<object?> GetModalPage(string modalType, string key, string lng) =>
        modalType switch
        {
            "selectLanguage" => await GetModalSelectLanguage(key, lng),
            _ => null
        };

    private async Task<object?> GetModalSelectLanguage(string key, string lng)
    {
        var cacheKey = string.Format(CacheKeys.PageModelKey, lng, key);

        var result = await redisService.GetCacheValue<object>(cacheKey);
        if (result is not null)
        {
            return result;
        }

        var language = LocaleHelper.ConvertLocaleToLanguage(lng);
        var model = await GeneratePageModalSelectLanguage(language);

        await redisService.SetCacheValue(cacheKey, model);

        return await redisService.GetCacheValue<object>(cacheKey);
    }

    private async Task<ModalSelectLanguagePage?> GeneratePageModalSelectLanguage(Language lng)
    {
        const string mainKey = "modal.selectlanguage";
        await using var context = await dmpContextFactory.CreateDbContextAsync();
        var modelStr = (await context.PageContents.FirstOrDefaultAsync(p => p.Language == lng && p.Key == mainKey))?.Content;

        return modelStr is not null ? JsonSerializer.Deserialize<ModalSelectLanguagePage>((string)modelStr) : null;
    }
}
