namespace DMP.BL.Services;

public interface IPageService
{
    Task<object?> GetJson(string path, string lng);
}
