using GameGuild.CQRS;


namespace GameGuild.Modules.Users.Queries;

/// <summary>
/// Query to get unified verification status for a user (email, phone, 2FA)
/// </summary>
public sealed record GetUnifiedVerificationStatusQuery(
    Guid UserId
) : IRequest<Result<UnifiedVerificationStatusDto>>;

/// <summary>
/// DTO representing the unified verification status of a user
/// </summary>
public sealed record UnifiedVerificationStatusDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Email verification status
    /// </summary>
    public bool IsEmailVerified { get; init; }

    /// <summary>
    /// Email address (if verified)
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Date when email was verified
    /// </summary>
    public DateTime? EmailVerifiedAt { get; init; }

    /// <summary>
    /// Phone verification status
    /// </summary>
    public bool IsPhoneVerified { get; init; }

    /// <summary>
    /// Phone number (if verified)
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Date when phone was verified
    /// </summary>
    public DateTime? PhoneVerifiedAt { get; init; }

    /// <summary>
    /// Two-factor authentication (MFA) status
    /// </summary>
    public bool IsTwoFactorEnabled { get; init; }

    /// <summary>
    /// Preferred MFA method
    /// </summary>
    public string? MfaMethod { get; init; }

    /// <summary>
    /// Date when 2FA was enabled
    /// </summary>
    public DateTime? TwoFactorEnabledAt { get; init; }

    /// <summary>
    /// Date when 2FA was last used
    /// </summary>
    public DateTime? TwoFactorLastUsedAt { get; init; }

    /// <summary>
    /// Overall account security score (0-100)
    /// </summary>
    public int SecurityScore { get; init; }

    /// <summary>
    /// Security recommendations based on verification status
    /// </summary>
    public List<string> SecurityRecommendations { get; init; } = [];

    /// <summary>
    /// Calculate security score based on verification status
    /// </summary>
    public static int CalculateSecurityScore(bool emailVerified, bool phoneVerified, bool mfaEnabled)
    {
        int score = 0;

        // Email verification: 30 points
        if (emailVerified) score += 30;

        // Phone verification: 20 points
        if (phoneVerified) score += 20;

        // MFA enabled: 50 points (highest weight)
        if (mfaEnabled) score += 50;

        return score;
    }

    /// <summary>
    /// Generate security recommendations
    /// </summary>
    public static List<string> GenerateRecommendations(bool emailVerified, bool phoneVerified, bool mfaEnabled)
    {
        var recommendations = new List<string>();

        if (!emailVerified)
            recommendations.Add("Verify your email address to improve account security and recovery options.");

        if (!phoneVerified)
            recommendations.Add("Add and verify a phone number for enhanced account recovery and SMS notifications.");

        if (!mfaEnabled)
            recommendations.Add("Enable two-factor authentication (2FA) for maximum account security.");

        if (emailVerified && phoneVerified && mfaEnabled)
            recommendations.Add("Your account has excellent security! Keep your recovery information up to date.");

        return recommendations;
    }
}
