using DMP.Crosscutting;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Web.Attributes;

/// <summary>
/// Fills the action's locale argument when it is not passed explicitly in the query string.
/// Resolution order: query parameter (used as is), locale cookie, first Accept-Language entry, "en".
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ExtractLocaleAttribute(string cookieName = "NEXT_LOCALE", string parameterName = "locale")
    : ActionFilterAttribute
{
    private const string DefaultLocale = "en";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;

        if (request.Query.TryGetValue(parameterName, out var queryLocale) && !string.IsNullOrEmpty(queryLocale))
        {
            return;
        }

        request.Cookies.TryGetValue(cookieName, out var localeValue);

        if (string.IsNullOrEmpty(localeValue))
        {
            var acceptLanguageHeader = request.Headers.AcceptLanguage.ToString();
            if (!string.IsNullOrEmpty(acceptLanguageHeader))
            {
                localeValue = acceptLanguageHeader.Split(',')[0].Split(';')[0];
            }
        }

        if (string.IsNullOrEmpty(localeValue))
        {
            localeValue = DefaultLocale;
        }

        context.ActionArguments[parameterName] = LocaleConverter.ConvertToSupportedLocale(localeValue);

        base.OnActionExecuting(context);
    }
}
