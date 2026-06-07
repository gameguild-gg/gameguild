using Microsoft.Extensions.Configuration;

namespace GameGuild.Configuration.PresentationLayer.RequestContext;

/// <summary>
///     Builder for request context options.
/// </summary>
public static class RequestContextOptionsBuilder
{
    /// <summary>
    ///     Creates request context options with default values.
    /// </summary>
    /// <returns>Default request context options</returns>
    public static RequestContextOptions Create() { return new RequestContextOptions { EnableUser = true, EnableTenant = true }; }

    /// <summary>
    ///     Creates request context options from a specific configuration section.
    /// </summary>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <returns>Configured request context options</returns>
    public static RequestContextOptions Create(IConfiguration configuration, string sectionName = "RequestContext")
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = Create();
        var section = configuration.GetSection(sectionName);

        if (section.Exists()) { section.Bind(options); }

        return options;
    }

    /// <summary>
    ///     Validates the provided request context options.
    /// </summary>
    /// <param name="options">The options to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null</exception>
    public static void Validate(RequestContextOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        // Request context options are generally valid with any boolean values
    }

    /// <summary>
    ///     Creates and validates request context options with default values.
    /// </summary>
    /// <returns>Validated request context options with default configuration</returns>
    public static RequestContextOptions Build()
    {
        var options = Create();
        Validate(options);

        return options;
    }

    /// <summary>
    ///     Creates and validates request context options from configuration.
    /// </summary>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <returns>Validated request context options</returns>
    public static RequestContextOptions Build(IConfiguration configuration, string sectionName = "RequestContext")
    {
        var options = Create(configuration, sectionName);
        Validate(options);

        return options;
    }
}
