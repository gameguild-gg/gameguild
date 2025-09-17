namespace GameGuild;

/// <summary>
/// Builder for creating InfrastructureLayerOptions from configuration.
/// </summary>
public static class InfrastructureLayerOptionsBuilder {
  /// <summary>
  /// Creates InfrastructureLayerOptions from configuration.
  /// </summary>
  /// <param name="configuration">The application configuration</param>
  /// <returns>Configured InfrastructureLayerOptions</returns>
  public static InfrastructureLayerOptions Create(IConfiguration configuration) {
    var options = new InfrastructureLayerOptions();

    var section = configuration.GetSection("InfrastructureLayer");

    if (section.Exists()) { section.Bind(options); }

    // Set defaults if not configured
    options.Database ??= DatabaseOptionsBuilder.CreateDefault(configuration);
    options.MessageQueue ??= new MessageQueueOptions();
    options.ExternalApis ??= new ExternalApiOptions();
    options.FileStorage ??= new FileStorageOptions();
    options.Monitoring ??= new MonitoringOptions();

    return options;
  }

  /// <summary>
  /// Creates InfrastructureLayerOptions with default values.
  /// </summary>
  /// <returns>InfrastructureLayerOptions with default configuration</returns>
  public static InfrastructureLayerOptions CreateDefault() {
    return new InfrastructureLayerOptions {
      EnableDatabase = true,
      Database = new DatabaseOptions(),
      EnableMessageQueue = false,
      MessageQueue = new MessageQueueOptions(),
      EnableExternalApis = false,
      ExternalApis = new ExternalApiOptions(),
      EnableFileStorage = false,
      FileStorage = new FileStorageOptions(),
      EnableMonitoring = true,
      Monitoring = new MonitoringOptions()
    };
  }
}
