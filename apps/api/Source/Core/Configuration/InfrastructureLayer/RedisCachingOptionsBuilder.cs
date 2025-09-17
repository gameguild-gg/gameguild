namespace GameGuild;

/// <summary> Builder for creating and configuring Redis caching options </summary>
public static class RedisCachingOptionsBuilder {
  /// <summary> Creates default Redis caching options </summary>
  /// <returns> Default Redis caching options </returns>
  public static RedisCachingOptions Create() {
    return new RedisCachingOptions {
      ConnectionString = "localhost:6379",
      InstanceName = "GameGuild",
      DefaultExpirationMinutes = 60,
      FeatureFlagExpirationMinutes = 15,
      UserSessionExpirationMinutes = 120,
      EnableHealthChecks = true,
      ConnectTimeoutMs = 5000,
      SyncTimeoutMs = 5000,
    };
  }

  /// <summary> Creates Redis caching options from configuration </summary>
  /// <param name="configuration"> Configuration instance </param>
  /// <param name="sectionName"> Configuration section name </param>
  /// <returns> Configured Redis caching options </returns>
  public static RedisCachingOptions Create(IConfiguration configuration, string sectionName = "RedisCache") {
    ArgumentNullException.ThrowIfNull(configuration);

    var options = Create();

    // Bind configuration values
    var section = configuration.GetSection(sectionName);

    if (section.Exists()) { section.Bind(options); }

    // Override with environment variables if present
    var connectionString = configuration.GetConnectionString("Redis") ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

    if (!string.IsNullOrWhiteSpace(connectionString)) { options.ConnectionString = connectionString; }

    return options;
  }

  /// <summary> Validates Redis caching options </summary>
  /// <param name="options"> Options to validate </param>
  /// <exception cref="ArgumentException"> Thrown when options are invalid </exception>
  public static void Validate(RedisCachingOptions options) {
    ArgumentNullException.ThrowIfNull(options);
    options.Validate();
  }

  /// <summary> Builds and validates default Redis caching options </summary>
  /// <returns> Built and validated Redis caching options </returns>
  public static RedisCachingOptions Build() {
    var options = Create();
    Validate(options);

    return options;
  }

  /// <summary> Builds and validates Redis caching options from configuration </summary>
  /// <param name="configuration"> Configuration instance </param>
  /// <param name="sectionName"> Configuration section name </param>
  /// <returns> Built and validated Redis caching options </returns>
  public static RedisCachingOptions Build(IConfiguration configuration, string sectionName = "RedisCache") {
    var options = Create(configuration, sectionName);
    Validate(options);

    return options;
  }
}
