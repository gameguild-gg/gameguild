namespace GameGuild;

public static class FeatureFlagsOptionsBuilder {
  public static FeatureFlagsOptions Create() { return new FeatureFlagsOptions { UseInMemoryProvider = true, DefaultDomain = null, BooleanFlags = new Dictionary<string, bool>() }; }

  public static FeatureFlagsOptions Create(IConfiguration configuration, string sectionName = "FeatureFlags") {
    ArgumentNullException.ThrowIfNull(configuration);

    var options = Create();
    var section = configuration.GetSection(sectionName);

    if (section.Exists()) { section.Bind(options); }

    return options;
  }

  public static FeatureFlagsOptions Build(this FeatureFlagsOptions options) {
    ArgumentNullException.ThrowIfNull(options);
    options.Validate();

    return options;
  }
}
