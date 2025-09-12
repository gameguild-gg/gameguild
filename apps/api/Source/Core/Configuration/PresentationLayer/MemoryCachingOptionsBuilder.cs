namespace GameGuild;

/// <summary>
/// Builder for memory caching options.
/// </summary>
public static class MemoryCachingOptionsBuilder
{
    /// <summary>
    /// Creates memory caching options with default values.
    /// </summary>
    /// <returns>Default memory caching options</returns>
    public static MemoryCachingOptions Create()
    {
        return new MemoryCachingOptions
        {
            SizeLimit = 100 * 1024 * 1024, // 100MB
            CompactionPercentage = 0.05, // 5%
            ExpirationScanFrequency = TimeSpan.FromMinutes(1)
        };
    }

    /// <summary>
    /// Creates memory caching options from a specific configuration section.
    /// </summary>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <returns>Configured memory caching options</returns>
    public static MemoryCachingOptions Create(IConfiguration configuration, string sectionName = "MemoryCaching")
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = Create();
        var section = configuration.GetSection(sectionName);

        if (section.Exists())
        {
            section.Bind(options);
        }

        return options;
    }

    /// <summary>
    /// Validates the provided memory caching options.
    /// </summary>
    /// <param name="options">The options to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    public static void Validate(MemoryCachingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.SizeLimit <= 0)
            throw new InvalidOperationException("Size limit must be greater than zero.");

        if (options.CompactionPercentage < 0 || options.CompactionPercentage > 1)
            throw new InvalidOperationException("Compaction percentage must be between 0 and 1.");

        if (options.ExpirationScanFrequency <= TimeSpan.Zero)
            throw new InvalidOperationException("Expiration scan frequency must be greater than zero.");
    }

    /// <summary>
    /// Creates and validates memory caching options with default values.
    /// </summary>
    /// <returns>Validated memory caching options with default configuration</returns>
    public static MemoryCachingOptions Build()
    {
        var options = Create();
        Validate(options);

        return options;
    }

    /// <summary>
    /// Creates and validates memory caching options from configuration.
    /// </summary>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <returns>Validated memory caching options</returns>
    public static MemoryCachingOptions Build(IConfiguration configuration, string sectionName = "MemoryCaching")
    {
        var options = Create(configuration, sectionName);
        Validate(options);

        return options;
    }
}
