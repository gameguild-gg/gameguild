namespace GameGuild.Configuration.PresentationLayer.Endpoints;

/// <summary>
///     Configuration options for Minimal API Endpoints
/// </summary>
public sealed class EndpointsOptions : BaseOptions
{
    /// <summary>
    ///     The configuration section name for this options type.
    /// </summary>
    public const string SectionName = "Endpoints";

    /// <summary>
    ///     Whether to register endpoints from the main API assembly
    /// </summary>
    public bool RegisterFromMainAssembly { get; set; } = true;

    /// <summary>
    ///     List of additional assembly names to scan for endpoints
    /// </summary>
    public string[] AdditionalAssemblies { get; set; } = [];

    public static EndpointsOptions CreateDefault()
    {
        return new EndpointsOptions();
    }
}
