namespace GameGuild.Configuration.PresentationLayer.OpenAPI;

public sealed class OpenApiOptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name for this options type.
    /// </summary>
    public const string SectionName = "OpenApi";

    public bool EnableOpenApi { get; set; } = true;

    public string Title { get; set; } = "GameGuild API";

    public string Version { get; set; } = "v1";

    /// <summary>
    ///     Overrides the release version shown in OpenAPI document metadata. Empty uses the assembly version.
    /// </summary>
    public string MetadataVersion { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ContactName { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;

    public string ContactUrl { get; set; } = string.Empty;

    public string TermsOfServiceUrl { get; set; } = string.Empty;

    public string LicenseName { get; set; } = string.Empty;

    public string LicenseUrl { get; set; } = string.Empty;

    /// <summary>
    ///     Servers advertised by generated OpenAPI documents.
    /// </summary>
    public List<OpenApiServerOptions> Servers { get; set; } = [];

    /// <summary>Retains the existing Authorization-header Bearer definition by default.</summary>
    public bool EnableDefaultBearer { get; set; } = true;

    /// <summary>Additional schemes, keyed by their OpenAPI security definition names.</summary>
    public Dictionary<string, OpenApiSecuritySchemeOptions> SecuritySchemes { get; set; } = new();

    public OpenApiUiOptions Ui { get; set; } = new();

    public override void Validate()
    {
        base.Validate();

        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new InvalidOperationException("OpenAPI title cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(Version))
        {
            throw new InvalidOperationException("OpenAPI document key cannot be null or empty.");
        }

        ValidateOptionalHttpUrl(ContactUrl, nameof(ContactUrl));
        ValidateOptionalHttpUrl(TermsOfServiceUrl, nameof(TermsOfServiceUrl));
        ValidateOptionalHttpUrl(LicenseUrl, nameof(LicenseUrl));

        if (!string.IsNullOrWhiteSpace(LicenseUrl) && string.IsNullOrWhiteSpace(LicenseName))
        {
            throw new InvalidOperationException("An OpenAPI license URL requires a license name.");
        }

        foreach (var server in Servers)
        {
            server.Validate();
        }

        if (Ui is null)
        {
            throw new InvalidOperationException("OpenAPI UI options cannot be null.");
        }

        Ui.Validate();

        if (SecuritySchemes is null)
        {
            throw new InvalidOperationException("OpenAPI security schemes cannot be null.");
        }

        if (SecuritySchemes.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != SecuritySchemes.Count)
        {
            throw new InvalidOperationException("OpenAPI security scheme names must be unique regardless of case.");
        }

        foreach (var (name, scheme) in SecuritySchemes)
        {
            if (EnableDefaultBearer && name.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The default Bearer scheme cannot be redefined while enabled.");
            }

            if (scheme is null)
            {
                throw new InvalidOperationException($"OpenAPI security scheme '{name}' cannot be null.");
            }

            scheme.Validate(name);
        }
    }

    private static void ValidateOptionalHttpUrl(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException($"OpenAPI {propertyName} must be an absolute HTTP or HTTPS URL.");
        }
    }

    public static OpenApiOptions CreateDefault() { return new OpenApiOptions(); }
}
