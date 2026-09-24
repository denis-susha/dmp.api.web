using Microsoft.AspNetCore.Mvc;

namespace API.Web.Helpers;

public static class ResponseHelper
{
    private const string DefaultNotFoundDetail = "Incorrect url.";

    /// <summary>
    /// Creates a new 404 <see cref="ProblemDetails"/> instance per call (a shared instance would leak custom details
    /// and framework-added extensions between requests).
    /// </summary>
    public static ProblemDetails NotFound(string detail = "") => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Resource not found",
        Detail = string.IsNullOrEmpty(detail) ? DefaultNotFoundDetail : detail
    };
}
