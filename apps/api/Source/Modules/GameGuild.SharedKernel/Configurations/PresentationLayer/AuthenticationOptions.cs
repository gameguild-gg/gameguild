namespace GameGuild.SharedKernel.Configuration;

/// <summary>
///     Configuration options for authentication and authorization
/// </summary>
public class AuthenticationOptions : BaseOptions
{
    public bool EnableAuthentication { get; set; } = true;

    public bool EnableAuthorization { get; set; } = true;

    public bool EnableDacAuthorization { get; set; } = true;

    public string JwtSecretKey { get; set; } = string.Empty;

    public string JwtIssuer { get; set; } = string.Empty;

    public string JwtAudience { get; set; } = string.Empty;

    public TimeSpan JwtExpiration { get; set; } = TimeSpan.FromHours(24);

    public override void Validate()
    {
        base.Validate();

        if (EnableAuthentication)
        {
            if (string.IsNullOrEmpty(JwtSecretKey)) throw new InvalidOperationException("JWT secret key must be configured when authentication is enabled.");

            if (string.IsNullOrEmpty(JwtIssuer)) throw new InvalidOperationException("JWT issuer must be configured when authentication is enabled.");

            if (string.IsNullOrEmpty(JwtAudience)) throw new InvalidOperationException("JWT audience must be configured when authentication is enabled.");

            if (JwtExpiration <= TimeSpan.Zero) throw new InvalidOperationException("JWT expiration must be greater than zero.");
        }
    }

    public static AuthenticationOptions CreateDefault() { return new AuthenticationOptions(); }
}
