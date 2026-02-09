using Microsoft.Extensions.Configuration;

namespace GameGuild.Configuration;

/// <summary>
///     Utility class providing common functionality for option builders.
///     This class contains shared logic to ensure consistency across all option builders.
/// </summary>
public static class OptionBuilderUtilities
{
    /// <summary>
    ///     Creates and binds an options object from configuration using a default factory.
    /// </summary>
    /// <typeparam name="T">The type of options to create</typeparam>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <param name="defaultFactory">Factory function to create default instance</param>
    /// <returns>Configured options instance</returns>
    public static T CreateAndBind<T>(IConfiguration configuration, string sectionName, Func<T> defaultFactory) where T : class
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(sectionName);
        ArgumentNullException.ThrowIfNull(defaultFactory);

        var options = defaultFactory();
        var section = configuration.GetSection(sectionName);

        if (section.Exists()) { section.Bind(options); }

        return options;
    }

    /// <summary>
    ///     Creates and binds an options object from configuration with validation.
    /// </summary>
    /// <typeparam name="T">The type of options to create</typeparam>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <param name="defaultFactory">Factory function to create default instance</param>
    /// <param name="validator">Optional validation function</param>
    /// <returns>Configured and validated options instance</returns>
    public static T CreateBindAndValidate<T>(IConfiguration configuration, string sectionName, Func<T> defaultFactory, Action<T>? validator = null) where T : class
    {
        var options = CreateAndBind(configuration, sectionName, defaultFactory);

        validator?.Invoke(options);

        return options;
    }
}

/// <summary>
///     Generic options builder that encapsulates the common Create → Bind → Validate pattern.
///     Use this to eliminate boilerplate in feature-specific option builders.
/// </summary>
/// <typeparam name="T">The options type, must extend <see cref="BaseOptions"/></typeparam>
public static class OptionsBuilder<T> where T : BaseOptions, new()
{
    /// <summary>
    ///     Creates a new options instance with defaults.
    /// </summary>
    public static T Create() => new();

    /// <summary>
    ///     Creates and binds an options instance from configuration.
    /// </summary>
    public static T Create(IConfiguration configuration, string sectionName)
        => OptionBuilderUtilities.CreateAndBind(configuration, sectionName, () => new T());

    /// <summary>
    ///     Creates, binds, validates, and returns an options instance.
    /// </summary>
    public static T Build(IConfiguration configuration, string sectionName)
        => OptionBuilderUtilities.CreateBindAndValidate(configuration, sectionName, () => new T(), o => o.Validate());
}
