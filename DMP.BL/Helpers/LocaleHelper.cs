using DMP.DataAccess.Models.Enumerations;

namespace DMP.BL.Helpers;

public static class LocaleHelper
{
    public static Language ConvertLocaleToLanguage(string locale) => locale switch
    {
        "ru" => Language.Russian,
        _ => Language.English
    };
}
