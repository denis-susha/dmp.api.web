using DMP.DataAccess.Models.Enumerations;

namespace DMP.DataAccess.Models;

public class BlogPostDAL
{
    public int BlogPostId { get; set; }
    public string Slug { get; set; } = null!;
    public Language Language { get; set; }
    public string CoverPath { get; set; } = null!;
    public DateTimeOffset PublishedAt { get; set; }
    public byte MinRead { get; set; }
    public string Title { get; set; } = null!;
    public string ShortContent { get; set; } = null!;
    public string Content { get; set; } = null!;
    public BlogPostAttributesDAL Attributes { get; set; } = null!;
}
