namespace GameGuild.Modules.Authentication;

/// <summary> JWT configuration options. </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 60;

    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary> Applies fallback values with warnings when environment variables are not set. </summary>
    /// <param name="configuration"> Application configuration </param>
    public void ApplyFallbacksWithWarnings(IConfiguration configuration)
    {
        var logger = GetLogger();

        // Debug logging for test troubleshooting
        logger?.LogInformation(
            "JWT Configuration Debug: SecretKey from config = '{SecretKey}', Issuer = '{Issuer}', Audience = '{Audience}'",
            configuration["Jwt:SecretKey"],
            configuration["Jwt:Issuer"],
            configuration["Jwt:Audience"]
        );

        if (string.IsNullOrEmpty(SecretKey))
        {
            SecretKey = "gameguild-production-jwt-secret-key-must-be-at-least-32-characters-long-and-secure";
            logger?.LogWarning("JWT SecretKey not found in configuration. Using fallback value. Please set Jwt__SecretKey environment variable for production.");
        }
        else { logger?.LogInformation("JWT SecretKey loaded from configuration: {SecretKeyLength} characters", SecretKey.Length); }

        if (string.IsNullOrEmpty(Issuer))
        {
            Issuer = "GameGuild";
            logger?.LogWarning("JWT Issuer not found in configuration. Using fallback value 'GameGuild'. Please set Jwt__Issuer environment variable.");
        }

        if (string.IsNullOrEmpty(Audience))
        {
            Audience = "GameGuild.Users";
            logger?.LogWarning("JWT Audience not found in configuration. Using fallback value 'GameGuild.Users'. Please set Jwt__Audience environment variable.");
        }

        if (ExpirationMinutes <= 0)
        {
            ExpirationMinutes = 15;
            logger?.LogWarning("JWT ExpirationMinutes not found or invalid in configuration. Using fallback value '15'. Please set Jwt__ExpirationMinutes environment variable.");
        }

        if (RefreshTokenExpirationDays <= 0)
        {
            RefreshTokenExpirationDays = 7;
            logger?.LogWarning("JWT RefreshTokenExpirationDays not found or invalid in configuration. Using fallback value '7'. Please set Jwt__RefreshTokenExpirationDays environment variable.");
        }
    }

    private ILogger? GetLogger()
    {
        try
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

            return loggerFactory.CreateLogger<JwtOptions>();
        }
        catch
        {
            // If logger creation fails, return null and continue without logging
            return null;
        }
    }

    public void Validate()
    {
        if (string.IsNullOrEmpty(SecretKey)) throw new InvalidOperationException("JWT SecretKey is required");
        if (string.IsNullOrEmpty(Issuer)) throw new InvalidOperationException("JWT Issuer is required");
        if (string.IsNullOrEmpty(Audience)) throw new InvalidOperationException("JWT Audience is required");
    }
}
