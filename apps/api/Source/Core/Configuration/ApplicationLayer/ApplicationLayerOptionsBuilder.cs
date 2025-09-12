namespace GameGuild;

/// <summary>
/// Builder for creating ApplicationLayerOptions from configuration.
/// </summary>
public static class ApplicationLayerOptionsBuilder
{
    /// <summary>
    /// Creates ApplicationLayerOptions with default values.
    /// </summary>
    /// <returns>ApplicationLayerOptions with default configuration</returns>
    public static ApplicationLayerOptions Create()
    {
        return new ApplicationLayerOptions
        {
            EnableMediatR = true,
            EnableAutoMapper = true,
            EnableFluentValidation = true,
            Caching = new CachingOptions(),
            BackgroundServices = new BackgroundServiceOptions()
        };
    }

    /// <summary>
    /// Creates ApplicationLayerOptions from a specific configuration section.
    /// </summary>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <returns>Configured ApplicationLayerOptions</returns>
    public static ApplicationLayerOptions Create(IConfiguration configuration, string sectionName = "ApplicationLayer")
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = Create();

        var section = configuration.GetSection(sectionName);
        if (section.Exists())
        {
            section.Bind(options);
        }

        // Set defaults if not configured
        options.Caching ??= new CachingOptions();
        options.BackgroundServices ??= new BackgroundServiceOptions();

        return options;
    }

    /// <summary>
    /// Validates the provided application layer options.
    /// </summary>
    /// <param name="options">The options to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null</exception>
    public static void Validate(ApplicationLayerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Validate nested options
        if (options.Caching != null)
        {
            options.Caching.Validate();
        }

        if (options.BackgroundServices != null)
        {
            options.BackgroundServices.Validate();
        }
    }

    /// <summary>
    /// Creates and validates ApplicationLayerOptions with default values.
    /// </summary>
    /// <returns>Validated ApplicationLayerOptions with default configuration</returns>
    public static ApplicationLayerOptions Build()
    {
        var options = Create();
        Validate(options);

        return options;
    }

    /// <summary>
    /// Creates and validates ApplicationLayerOptions from configuration.
    /// </summary>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <returns>Validated ApplicationLayerOptions</returns>
    public static ApplicationLayerOptions Build(IConfiguration configuration, string sectionName = "ApplicationLayer")
    {
        var options = Create(configuration, sectionName);
        Validate(options);

        return options;
    }
}
