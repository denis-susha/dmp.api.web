
namespace DMP.BL.Models.Blog;

public class BlogPost
{
    public int BlogPostId { get; set; }
    public string Slug { get; set; } = null!;
    public string CoverPath { get; set; } = null!;
    public DateTimeOffset PublishedAt { get; set; }
    public byte MinRead { get; set; }
    public string Title { get; set; } = null!;
    public string? ShortContent { get; set; }
    public string? Content { get; set; }
    public BlogPostAttributes? Attributes { get; set; }
}
