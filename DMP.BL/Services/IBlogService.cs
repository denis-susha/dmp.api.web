using DMP.BL.Models.Blog;

namespace DMP.BL.Services;

public interface IBlogService
{
    Task<List<BlogPost>> GetPosts(string locale);
    Task<BlogPost?> GetPost(string locale, string slug);
}
