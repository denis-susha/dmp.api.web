namespace API.Web.Attributes;

/// <summary>Hides an action parameter from the generated OpenAPI document.</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class HideFromOpenApiAttribute : Attribute;
