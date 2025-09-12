namespace GameGuild;

/// <summary>
/// Builder for creating AuthenticationOptions from configuration.
/// </summary>
public static class AuthenticationOptionsBuilder
{
    /// <summary>
    /// Creates AuthenticationOptions with default values.
    /// </summary>
    /// <returns>AuthenticationOptions with default configuration</returns>
    public static AuthenticationOptions Create()
    {
        return new AuthenticationOptions
        {
            EnableAuthentication = true,
            EnableAuthorization = true,
            EnableDacAuthorization = true,
            JwtSecretKey = "development-secret-key-32-chars!!",
            JwtIssuer = "GameGuild",
            JwtAudience = "GameGuild",
            JwtExpiration = TimeSpan.FromHours(8)
        };
    }

    /// <summary>
    /// Creates AuthenticationOptions from a specific configuration section.
    /// </summary>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <returns>Configured AuthenticationOptions</returns>
    public static AuthenticationOptions Create(IConfiguration configuration, string sectionName = "Authentication")
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
    /// Validates the provided authentication options.
    /// </summary>
    /// <param name="options">The options to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid</exception>
    public static void Validate(AuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.EnableAuthentication)
        {
            if (string.IsNullOrWhiteSpace(options.JwtSecretKey))
                throw new InvalidOperationException("JWT secret key is required when authentication is enabled.");

            if (options.JwtSecretKey.Length < 32)
                throw new InvalidOperationException("JWT secret key must be at least 32 characters long for security.");

            if (string.IsNullOrWhiteSpace(options.JwtIssuer))
                throw new InvalidOperationException("JWT issuer is required when authentication is enabled.");

            if (string.IsNullOrWhiteSpace(options.JwtAudience))
                throw new InvalidOperationException("JWT audience is required when authentication is enabled.");

            if (options.JwtExpiration <= TimeSpan.Zero)
                throw new InvalidOperationException("JWT expiration must be greater than zero.");
        }
    }

    /// <summary>
    /// Creates and validates AuthenticationOptions with default values.
    /// </summary>
    /// <returns>Validated AuthenticationOptions with default configuration</returns>
    public static AuthenticationOptions Build()
    {
        var options = Create();
        Validate(options);

        return options;
    }

    /// <summary>
    ///    Creates and validates AuthenticationOptions from configuration.
    /// </summary>
    /// <param name="configuration">The configuration to bind from</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <returns>Validated AuthenticationOptions</returns>
    public static AuthenticationOptions Build(IConfiguration configuration, string sectionName = "Authentication")
    {
        var options = Create(configuration, sectionName);
        Validate(options);

        return options;
    }

    /// <summary>
    ///    Creates authentication options with authentication disabled for testing.
    /// </summary>
    public static AuthenticationOptions CreateDisabled()
    {
        return new AuthenticationOptions
        {
            EnableAuthentication = false, EnableAuthorization = false, EnableDacAuthorization = false
        };
    }

    /// <summary>
    ///    Creates authentication options with development-friendly settings.
    /// </summary>
    public static AuthenticationOptions CreateDevelopment(string secretKey = "development-secret-key-32-chars!!")
    {
        return new AuthenticationOptions
        {
            EnableAuthentication = true,
            EnableAuthorization = true,
            EnableDacAuthorization = true,
            JwtSecretKey = secretKey,
            JwtIssuer = "GameGuild-Dev",
            JwtAudience = "GameGuild-Dev",
            JwtExpiration = TimeSpan.FromHours(24)
        };
    }
}
