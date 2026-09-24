using API.Web.Attributes;
using DMP.BL.Models.Blog;
using DMP.BL.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class BlogController(ILogger<BlogController> logger, IBlogService blogService) : ControllerBase
{
    [HttpGet]
    [ExtractLocale]
    public async Task<ActionResult<List<BlogPost>>> GetPosts([HideFromOpenApi] string locale)
    {
        try
        {
            var posts = await blogService.GetPosts(locale);

            if (!posts.Any())
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource not found"
                });
            }

            return Ok(posts);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get blog posts for locale {Locale}", locale);
            return StatusCode(500);
        }
    }

    [HttpGet]
    [Route("post")]
    [ExtractLocale]
    public async Task<ActionResult<BlogPost>> GetPost([FromQuery] string slug,
        [HideFromOpenApi] string locale)
    {
        try
        {
            var post = await blogService.GetPost(locale, slug);

            if (post is null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource not found",
                    Detail = $"Resource with slug {slug} was not found."
                });
            }

            return Ok(post);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get blog post {Slug} for locale {Locale}", slug, locale);
            return StatusCode(500);
        }
    }
}
