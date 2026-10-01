namespace GameGuild.Configuration.PresentationLayer.OpenAPI;

public enum OpenApiSecuritySchemeKind
{
    ApiKeyHeader,
    HttpBearer,
    HttpBasic,
    OAuth2AuthorizationCode
}

/// <summary>
///     Describes an opt-in security scheme exposed by generated OpenAPI documents.
/// </summary>
public sealed class OpenApiSecuritySchemeOptions
{
    public OpenApiSecuritySchemeKind Kind { get; set; } = OpenApiSecuritySchemeKind.ApiKeyHeader;

    public string Description { get; set; } = string.Empty;

    /// <summary>The ASP.NET authentication scheme used by explicitly attributed endpoints.</summary>
    public string AuthenticationScheme { get; set; } = string.Empty;

    public string HeaderName { get; set; } = "X-API-Key";

    public string AuthorizationUrl { get; set; } = string.Empty;

    public string TokenUrl { get; set; } = string.Empty;

    public Dictionary<string, string> Scopes { get; set; } = new();

    public void Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("An OpenAPI security scheme name cannot be empty.");
        }

        if (!Enum.IsDefined(Kind))
        {
            throw new InvalidOperationException($"OpenAPI security scheme '{name}' has an unsupported kind.");
        }

        if (Kind == OpenApiSecuritySchemeKind.ApiKeyHeader &&
            (string.IsNullOrWhiteSpace(HeaderName) || !HeaderName.All(character =>
                char.IsLetterOrDigit(character) || character == '-')))
        {
            throw new InvalidOperationException($"OpenAPI security scheme '{name}' requires a valid header name.");
        }

        if (Kind != OpenApiSecuritySchemeKind.OAuth2AuthorizationCode)
        {
            return;
        }

        ValidateHttpUrl(AuthorizationUrl, name, nameof(AuthorizationUrl));
        ValidateHttpUrl(TokenUrl, name, nameof(TokenUrl));
        if (Scopes is null || Scopes.Any(scope => string.IsNullOrWhiteSpace(scope.Key)))
        {
            throw new InvalidOperationException($"OpenAPI OAuth2 security scheme '{name}' has an invalid scope.");
        }
    }

    private static void ValidateHttpUrl(string value, string name, string property)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException($"OpenAPI security scheme '{name}' requires an absolute HTTP or HTTPS {property}.");
        }
    }
}
