using API.Web.Attributes;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace API.Web.OpenApi;

/// <summary>
/// Removes action parameters marked with <see cref="HideFromOpenApiAttribute"/> from the OpenAPI document.
/// Such parameters are filled by action filters (e.g. the locale), not by API clients.
/// </summary>
public sealed class HideParameterTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (operation.Parameters is not { Count: > 0 } ||
            context.Description.ActionDescriptor is not ControllerActionDescriptor actionDescriptor)
        {
            return Task.CompletedTask;
        }

        var hiddenParameters = actionDescriptor.MethodInfo.GetParameters()
            .Where(p => p.IsDefined(typeof(HideFromOpenApiAttribute), false))
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (hiddenParameters.Count > 0)
        {
            operation.Parameters = [.. operation.Parameters.Where(p => !hiddenParameters.Contains(p.Name ?? string.Empty))];
        }

        return Task.CompletedTask;
    }
}
