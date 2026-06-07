using Microsoft.Extensions.Configuration;

namespace GameGuild.Configuration.PresentationLayer.Authorization;

/// <summary>
///     Builder for authorization options.
/// </summary>
public static class AuthorizationOptionsBuilder
{
    /// <summary>
    ///     Creates authorization options with default values.
    /// </summary>
    /// <returns>Default authorization options</returns>
    public static AuthorizationOptions Create() { return new AuthorizationOptions { DefaultPolicy = "Default", RequireAuthenticatedUser = true }; }

    /// <summary>
    ///     Creates authorization options from a specific configuration section.
    /// </summary>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <returns>Configured authorization options</returns>
    public static AuthorizationOptions Create(IConfiguration configuration, string sectionName = "Authorization") { return OptionBuilderUtilities.CreateAndBind(configuration, sectionName, Create); }

    /// <summary>
    ///     Validates the provided authorization options.
    /// </summary>
    /// <param name="options">The options to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    public static void Validate(AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.DefaultPolicy)) throw new InvalidOperationException("Default policy name cannot be null or empty.");
    }

    /// <summary>
    ///     Creates and validates authorization options with default values.
    /// </summary>
    /// <returns>Validated authorization options with default configuration</returns>
    public static AuthorizationOptions Build()
    {
        var options = Create();
        Validate(options);

        return options;
    }

    /// <summary>
    ///     Creates and validates authorization options from configuration.
    /// </summary>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <returns>Validated authorization options</returns>
    public static AuthorizationOptions Build(IConfiguration configuration, string sectionName = "Authorization")
    {
        var options = Create(configuration, sectionName);
        Validate(options);

        return options;
    }
}
