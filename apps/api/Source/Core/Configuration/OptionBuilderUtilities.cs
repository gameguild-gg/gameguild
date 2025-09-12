namespace GameGuild;

/// <summary>
/// Utility class providing common functionality for option builders.
/// This class contains shared logic to ensure consistency across all option builders.
/// </summary>
internal static class OptionBuilderUtilities {
  /// <summary>
  /// Creates and binds an options object from configuration using a default factory.
  /// </summary>
  /// <typeparam name="T">The type of options to create</typeparam>
  /// <param name="configuration">The configuration to bind from</param>
  /// <param name="sectionName">The configuration section name</param>
  /// <param name="defaultFactory">Factory function to create default instance</param>
  /// <returns>Configured options instance</returns>
  public static T CreateAndBind<T>(IConfiguration configuration, string sectionName, Func<T> defaultFactory) where T : class {
    ArgumentNullException.ThrowIfNull(configuration);
    ArgumentNullException.ThrowIfNull(sectionName);
    ArgumentNullException.ThrowIfNull(defaultFactory);

    var options = defaultFactory();
    var section = configuration.GetSection(sectionName);

    if (section.Exists()) section.Bind(options);

    return options;
  }

  /// <summary>
  /// Creates and binds an options object from configuration with validation.
  /// </summary>
  /// <typeparam name="T">The type of options to create</typeparam>
  /// <param name="configuration">The configuration to bind from</param>
  /// <param name="sectionName">The configuration section name</param>
  /// <param name="defaultFactory">Factory function to create default instance</param>
  /// <param name="validator">Optional validation function</param>
  /// <returns>Configured and validated options instance</returns>
  public static T CreateBindAndValidate<T>(IConfiguration configuration, string sectionName, Func<T> defaultFactory, Action<T>? validator = null) where T : class {
    var options = CreateAndBind(configuration, sectionName, defaultFactory);

    validator?.Invoke(options);

    return options;
  }
}
