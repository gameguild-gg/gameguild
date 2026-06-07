namespace GameGuild.Configuration.PresentationLayer.OpenAPI;

/// <summary>
///     Configuration for OpenAPI server variables.
/// </summary>
public sealed class OpenApiServerVariableOptions: BaseOptions
{
    /// <summary>
    ///     The default value for the variable.
    /// </summary>
    public string Default { get; set; } = string.Empty;

    /// <summary>
    ///     The description of the variable.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Possible values for the variable.
    /// </summary>
    public string[ ] Enum { get; set; } = Array.Empty<string>();

    /// <summary>
    ///     Validates the server variable options.
    /// </summary>
    public new void Validate()
    {
        if (string.IsNullOrWhiteSpace(Default)) { throw new ArgumentException("Server variable default value cannot be null or empty.", nameof(Default)); }
    }
    
    public static OpenApiServerVariableOptions CreateDefault() { return new OpenApiServerVariableOptions(); }
}
