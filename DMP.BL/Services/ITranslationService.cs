namespace DMP.BL.Services;

public interface ITranslationService
{
    Task<string?> GetTranslation(string key, string lng);
    Task<string> GetMenuTranslation(bool isCategory, int id, string name, string lng = "en");
}
