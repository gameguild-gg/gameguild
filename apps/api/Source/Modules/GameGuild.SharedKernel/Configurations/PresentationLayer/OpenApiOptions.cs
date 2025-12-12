namespace GameGuild.SharedKernel.Configuration;

public class OpenApiOptions : BaseOptions
{
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
