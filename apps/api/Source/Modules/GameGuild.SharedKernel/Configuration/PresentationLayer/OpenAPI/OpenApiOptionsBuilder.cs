using Microsoft.Extensions.Configuration;

namespace GameGuild.Configuration.PresentationLayer.OpenAPI;

/// <summary>
///     Builder for creating OpenApiOptions from configuration.
/// </summary>
public static class OpenApiOptionsBuilder
{
    /// <summary>
    ///     Creates OpenApiOptions with default values.
    /// </summary>
    /// <returns>OpenApiOptions with default configuration</returns>
    public static OpenApiOptions Create() { return new OpenApiOptions(); }

    /// <summary>
    ///     Creates OpenApiOptions from a specific configuration section.
    /// </summary>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <returns>Configured OpenApiOptions</returns>
    public static OpenApiOptions Create(IConfiguration configuration, string sectionName = "OpenApi")
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = Create();

        var section = configuration.GetSection(sectionName);

        if (section.Exists()) { section.Bind(options); }

        return options;
    }

    /// <summary>
    ///     Validates the provided OpenAPI options.
    /// </summary>
    /// <param name="options">The options to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    public static void Validate(OpenApiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Title)) throw new InvalidOperationException("OpenAPI title cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(options.Version)) throw new InvalidOperationException("OpenAPI version cannot be null or empty.");
    }

    /// <summary>
    ///     Creates and validates OpenApiOptions with default values.
    /// </summary>
    /// <returns>Validated OpenApiOptions with default configuration</returns>
    public static OpenApiOptions Build()
    {
        var options = Create();
        Validate(options);

        return options;
    }

    /// <summary>
    ///     Creates and validates OpenApiOptions from configuration.
    /// </summary>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <returns>Validated OpenApiOptions</returns>
    public static OpenApiOptions Build(IConfiguration configuration, string sectionName = "OpenApi")
    {
        var options = Create(configuration, sectionName);
        Validate(options);

        return options;
    }
}
