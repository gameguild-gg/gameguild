namespace GameGuild.Configuration.PresentationLayer.Authentication;

/// <summary>
///     Configurable password requirements applied to local account registration.
/// </summary>
public sealed class AuthenticationPasswordPolicySettings
{
    public int MinPasswordLength { get; set; } = 8;

    public int MaxPasswordLength { get; set; } = 128;

    public bool RequireUppercase { get; set; } = true;

    public bool RequireLowercase { get; set; } = true;

    public bool RequireDigit { get; set; } = true;

    public bool RequireSpecialChar { get; set; } = true;

    public void Validate()
    {
        if (MinPasswordLength < 8)
        {
            throw new InvalidOperationException("Minimum password length must be at least 8 characters.");
        }

        if (MaxPasswordLength < MinPasswordLength)
        {
            throw new InvalidOperationException("Maximum password length must be greater than or equal to the minimum password length.");
        }

        if (MaxPasswordLength > 128)
        {
            throw new InvalidOperationException("Maximum password length must not exceed 128 characters.");
        }
    }
}
