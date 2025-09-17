namespace GameGuild;

/// <summary>
/// Static builder for health check configuration options.
/// </summary>
public static class HealthCheckOptionsBuilder {
  /// <summary>
  /// Creates a new health check options builder.
  /// </summary>
  public static HealthCheckOptionsBuilderInstance Create(IConfiguration configuration) { return new HealthCheckOptionsBuilderInstance(configuration); }

  /// <summary>
  /// Creates default health check options from configuration.
  /// </summary>
  public static HealthCheckOptions CreateDefault(IConfiguration configuration) { return Create(configuration).UseDefaultConfiguration().Build(); }

  /// <summary>
  /// Creates health check options with all checks enabled.
  /// </summary>
  public static HealthCheckOptions CreateAllEnabled() { return new HealthCheckOptions { EnableDatabaseCheck = true, EnableApiHealthCheck = true, Timeout = TimeSpan.FromSeconds(30) }; }

  /// <summary>
  /// Creates health check options with all checks disabled.
  /// </summary>
  public static HealthCheckOptions CreateDisabled() { return new HealthCheckOptions { EnableDatabaseCheck = false, EnableApiHealthCheck = false, Timeout = TimeSpan.FromSeconds(5) }; }

  public class HealthCheckOptionsBuilderInstance {
    private readonly IConfiguration _configuration;

    private readonly HealthCheckOptions _options = new HealthCheckOptions();

    internal HealthCheckOptionsBuilderInstance(IConfiguration configuration) { _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration)); }

    public HealthCheckOptionsBuilderInstance EnableDatabaseCheck(bool enable = true) {
      _options.EnableDatabaseCheck = enable;

      return this;
    }

    public HealthCheckOptionsBuilderInstance EnableApiHealthCheck(bool enable = true) {
      _options.EnableApiHealthCheck = enable;

      return this;
    }

    public HealthCheckOptionsBuilderInstance EnableAllChecks() {
      _options.EnableDatabaseCheck = true;
      _options.EnableApiHealthCheck = true;

      return this;
    }

    public HealthCheckOptionsBuilderInstance DisableAllChecks() {
      _options.EnableDatabaseCheck = false;
      _options.EnableApiHealthCheck = false;

      return this;
    }

    public HealthCheckOptionsBuilderInstance WithTimeout(TimeSpan timeout) {
      _options.Timeout = timeout;

      return this;
    }

    public HealthCheckOptionsBuilderInstance WithTimeoutInSeconds(int seconds) {
      _options.Timeout = TimeSpan.FromSeconds(seconds);

      return this;
    }

    public HealthCheckOptionsBuilderInstance WithTimeoutInMinutes(int minutes) {
      _options.Timeout = TimeSpan.FromMinutes(minutes);

      return this;
    }

    public HealthCheckOptionsBuilderInstance AddTag(string key, string value) {
      _options.Tags[key] = value;

      return this;
    }

    public HealthCheckOptionsBuilderInstance AddTags(Dictionary<string, string> tags) {
      foreach (var tag in tags) { _options.Tags[tag.Key] = tag.Value; }

      return this;
    }

    public HealthCheckOptionsBuilderInstance RemoveTag(string key) {
      _options.Tags.Remove(key);

      return this;
    }

    public HealthCheckOptionsBuilderInstance ClearTags() {
      _options.Tags.Clear();

      return this;
    }

    public HealthCheckOptionsBuilderInstance WithDevelopmentSettings() {
      _options.EnableDatabaseCheck = true;
      _options.EnableApiHealthCheck = true;
      _options.Timeout = TimeSpan.FromSeconds(60);

      return this;
    }

    public HealthCheckOptionsBuilderInstance WithProductionSettings() {
      _options.EnableDatabaseCheck = true;
      _options.EnableApiHealthCheck = true;
      _options.Timeout = TimeSpan.FromSeconds(30);

      return this;
    }

    public HealthCheckOptionsBuilderInstance UseDefaultConfiguration() {
      _configuration.GetSection("HealthChecks").Bind(_options);

      return this;
    }

    public HealthCheckOptionsBuilderInstance Reset() {
      _options.EnableDatabaseCheck = true;
      _options.EnableApiHealthCheck = true;
      _options.Timeout = TimeSpan.FromSeconds(30);
      _options.Tags.Clear();
      _options.Tags.Add("database", "infrastructure");
      _options.Tags.Add("api", "readiness");

      return this;
    }

    public HealthCheckOptions Build() { return _options; }
  }
}
