using Asp.Versioning;
using Microsoft.Extensions.Configuration;
using SharedApiVersioningOptions = GameGuild.Configuration.PresentationLayer.ApiVersioning.ApiVersioningOptions;

namespace GameGuild.Configuration.PresentationLayer.ApiVersioning;

/// <summary>
///     Builder for creating ApiVersioningOptions from configuration.
/// </summary>
public static class ApiVersioningOptionsBuilder
{
    /// <summary>
    ///     Creates ApiVersioningOptions with default values.
    /// </summary>
    /// <returns>ApiVersioningOptions with default configuration</returns>
    public static SharedApiVersioningOptions Create() { return new SharedApiVersioningOptions(); }

    /// <summary>
    ///     Creates ApiVersioningOptions from a specific configuration section.
    /// </summary>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <returns>Configured ApiVersioningOptions</returns>
    public static SharedApiVersioningOptions Create(IConfiguration configuration, string sectionName = "ApiVersioning")
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = Create();

        var section = configuration.GetSection(sectionName);

        if (section.Exists()) { section.Bind(options); }

        return options;
    }

    /// <summary>
    ///     Validates the provided API versioning options.
    /// </summary>
    /// <param name="options">The options to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    public static void Validate(SharedApiVersioningOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();
    }

    /// <summary>
    ///     Creates and validates ApiVersioningOptions with default values.
    /// </summary>
    /// <returns>Validated ApiVersioningOptions with default configuration</returns>
    public static SharedApiVersioningOptions Build()
    {
        var options = Create();
        Validate(options);

        return options;
    }

    /// <summary>
    ///     Creates and validates ApiVersioningOptions from configuration.
    /// </summary>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <returns>Validated ApiVersioningOptions</returns>
    public static SharedApiVersioningOptions Build(IConfiguration configuration, string sectionName = "ApiVersioning")
    {
        var options = Create(configuration, sectionName);
        Validate(options);

        return options;
    }

    /// <summary>
    ///     Converts ApiVersionReadingStrategy to ApiVersionReader.
    /// </summary>
    /// <param name="strategy">The reading strategy</param>
    /// <param name="options">The API versioning options</param>
    /// <returns>The configured ApiVersionReader</returns>
    public static IApiVersionReader CreateReader(ApiVersionReadingStrategy strategy, SharedApiVersioningOptions options)
    {
        return strategy switch
        {
            ApiVersionReadingStrategy.UrlSegment => new UrlSegmentApiVersionReader(),
            ApiVersionReadingStrategy.QueryString => new QueryStringApiVersionReader(options.QueryParameterName),
            ApiVersionReadingStrategy.Header => new HeaderApiVersionReader(options.HeaderName),
            ApiVersionReadingStrategy.MediaType => new MediaTypeApiVersionReader(options.MediaTypeParameterName),
            ApiVersionReadingStrategy.UrlSegmentAndQueryString => ApiVersionReader.Combine(new UrlSegmentApiVersionReader(), new QueryStringApiVersionReader(options.QueryParameterName)),
            ApiVersionReadingStrategy.UrlSegmentAndHeader => ApiVersionReader.Combine(new UrlSegmentApiVersionReader(), new HeaderApiVersionReader(options.HeaderName)),
            ApiVersionReadingStrategy.All => ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new QueryStringApiVersionReader(options.QueryParameterName),
                new HeaderApiVersionReader(options.HeaderName),
                new MediaTypeApiVersionReader(options.MediaTypeParameterName)
            ),
            _ => new UrlSegmentApiVersionReader()
        };
    }
}
