using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using DMP.BL.Models.AppSettings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace API.Web.Attributes;

/// <summary>
/// Protects service-to-service endpoints with HTTP Basic credentials from the "BasicAuthorize" configuration section.
/// Any missing, malformed or mismatching credentials result in 403.
/// </summary>
public class BasicAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private const string BasicScheme = "Basic";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<BasicAuthorizeOptions>>().Value;

        if (!TryGetCredentials(context.HttpContext.Request, out var username, out var password))
        {
            context.Result = new ForbidResult();
            return;
        }

        // Non-short-circuiting '&' so both comparisons always run (no timing hint about which one failed).
        var isValid = FixedTimeEquals(options.User, username) & FixedTimeEquals(options.Password, password);

        if (!isValid)
        {
            context.Result = new ForbidResult();
        }
    }

    private static bool TryGetCredentials(HttpRequest request,
        [NotNullWhen(true)] out string? username, [NotNullWhen(true)] out string? password)
    {
        username = null;
        password = null;

        var header = request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(header)
            || !AuthenticationHeaderValue.TryParse(header, out var authHeader)
            || !string.Equals(authHeader.Scheme, BasicScheme, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(authHeader.Parameter))
        {
            return false;
        }

        var buffer = new byte[authHeader.Parameter.Length * 3 / 4 + 3];
        if (!Convert.TryFromBase64String(authHeader.Parameter, buffer, out var bytesWritten))
        {
            return false;
        }

        var credentials = Encoding.UTF8.GetString(buffer, 0, bytesWritten);
        var separatorIndex = credentials.IndexOf(':');

        if (separatorIndex < 0)
        {
            return false;
        }

        username = credentials[..separatorIndex];
        password = credentials[(separatorIndex + 1)..];
        return true;
    }

    private static bool FixedTimeEquals(string expected, string actual) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(actual));
}
