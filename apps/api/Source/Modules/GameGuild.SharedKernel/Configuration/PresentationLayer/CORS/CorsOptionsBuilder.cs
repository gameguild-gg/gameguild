using Microsoft.Extensions.Configuration;

namespace GameGuild.Configuration.PresentationLayer.CORS;

/// <summary>
///     Builder for CORS options following the standard Create/Validate/Build pattern.
/// </summary>
public static class CorsOptionsBuilder
{
    /// <summary>
    ///     Creates CORS options with default values.
    /// </summary>
    /// <returns>Default CORS options</returns>
    public static CorsOptions Create()
    {
        return new CorsOptions { AllowedOrigins = [], AllowedMethods = [], AllowedHeaders = [] };
    }

    /// <summary>
    ///     Creates CORS options from configuration.
    /// </summary>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name (defaults to "Cors")</param>
    /// <returns>Configured CORS options</returns>
    public static CorsOptions Create(IConfiguration configuration, string sectionName = CorsOptions.SectionName)
        => OptionsBuilder<CorsOptions>.Create(configuration, sectionName);

    /// <summary>
    ///     Builds and validates CORS options.
    /// </summary>
    /// <param name="options">The options to validate and return</param>
    /// <returns>Validated and configured CORS options</returns>
    public static CorsOptions Build(this CorsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        return options;
    }
}