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

    public string Description { get; set; } = string.Empty;

    public string ContactName { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;

    public string ContactUrl { get; set; } = string.Empty;

    public string TermsOfServiceUrl { get; set; } = string.Empty;

    public string LicenseName { get; set; } = string.Empty;

    public string LicenseUrl { get; set; } = string.Empty;

    public static OpenApiOptions CreateDefault() { return new OpenApiOptions(); }
}
