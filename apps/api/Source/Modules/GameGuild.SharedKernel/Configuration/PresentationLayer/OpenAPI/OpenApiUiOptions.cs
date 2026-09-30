namespace GameGuild.Configuration.PresentationLayer.OpenAPI;

/// <summary>
///     Optional Swagger UI settings. Null flags retain the installed UI's defaults.
/// </summary>
public sealed class OpenApiUiOptions
{
    public string RoutePrefix { get; set; } = "documentation";

    public string DocumentTitle { get; set; } = string.Empty;

    public bool? EnableDeepLinking { get; set; }

    public bool? EnableFilter { get; set; }

    public bool? DisplayRequestDuration { get; set; }

    public bool? PersistAuthorization { get; set; }

    public void Validate()
    {
        if (RoutePrefix is null || RoutePrefix.StartsWith('/') || RoutePrefix.EndsWith('/') ||
            RoutePrefix.Contains("..", StringComparison.Ordinal) || RoutePrefix.Contains('\\') ||
            RoutePrefix.Contains('?') || RoutePrefix.Contains('#'))
        {
            throw new InvalidOperationException("OpenAPI UI route prefix must be a relative path without traversal or query characters.");
        }
    }
}
