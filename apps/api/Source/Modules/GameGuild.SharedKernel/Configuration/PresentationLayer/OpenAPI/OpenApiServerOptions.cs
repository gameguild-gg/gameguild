namespace GameGuild.Configuration.PresentationLayer.OpenAPI;

/// <summary>
///     Configuration for OpenAPI server information.
/// </summary>
public sealed class OpenApiServerOptions : BaseOptions
{
    /// <summary>
    ///     The server URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    ///     The server description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Server variables for templated URLs.
    /// </summary>
    public Dictionary<string, OpenApiServerVariableOptions> Variables { get; set; } = new();

    /// <summary>
    ///     Validates the server options.
    /// </summary>
    public new void Validate()
    {
        if (string.IsNullOrWhiteSpace(Url)) { throw new ArgumentException("Server URL cannot be null or empty.", nameof(Url)); }

        foreach (var variable in Variables.Values) { variable.Validate(); }
    }
    
    public static OpenApiServerOptions CreateDefault() { return new OpenApiServerOptions(); }
}
