using DMP.BL.Helpers;
using DMP.BL.Models.Blog;
using DMP.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace DMP.BL.Services;

public class BlogService(IDbContextFactory<DmpDbContext> dmpContextFactory) : IBlogService
{
    public async Task<List<BlogPost>> GetPosts(string locale)
    {
        var lng = LocaleHelper.ConvertLocaleToLanguage(locale);

        await using var context = await dmpContextFactory.CreateDbContextAsync();
        return await context.BlogPosts.Where(p => p.Language == lng).AsNoTracking().Select(p => new BlogPost
        {
            BlogPostId = p.BlogPostId,
            Slug = p.Slug,
            CoverPath = p.CoverPath,
            PublishedAt = p.PublishedAt,
            MinRead = p.MinRead,
            Title = p.Title,
            ShortContent = p.ShortContent,
        }).ToListAsync();
    }

    public async Task<BlogPost?> GetPost(string locale, string slug)
    {
        var lng = LocaleHelper.ConvertLocaleToLanguage(locale);

        await using var context = await dmpContextFactory.CreateDbContextAsync();
        return await context.BlogPosts.Where(p => p.Language == lng && p.Slug == slug).AsNoTracking().Select(p => new BlogPost
        {
            BlogPostId = p.BlogPostId,
            Slug = p.Slug,
            CoverPath = p.CoverPath,
            PublishedAt = p.PublishedAt,
            MinRead = p.MinRead,
            Title = p.Title,
            Content = p.Content,
            Attributes = new BlogPostAttributes
            {
                MetaTitle = p.Attributes.MetaTitle,
                MetaDescription = p.Attributes.MetaDescription,
            }
        }).FirstOrDefaultAsync();
    }
}
