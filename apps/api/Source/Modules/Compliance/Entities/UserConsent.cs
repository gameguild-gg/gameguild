using GameGuild.Core.Entities;

namespace GameGuild.Modules.Compliance.Entities;

/// <summary>
/// Represents a user's consent to a specific policy version.
/// </summary>
public sealed class UserConsent : EntityBase
{
    /// <summary>
    /// Gets or sets the user ID who gave consent.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the policy ID.
    /// </summary>
    public Guid PolicyId { get; set; }

    /// <summary>
    /// Gets or sets the policy version ID.
    /// </summary>
    public Guid PolicyVersionId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for multi-tenant support.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets whether the user consented (true) or declined (false).
    /// </summary>
    public bool IsConsented { get; set; }

    /// <summary>
    /// Gets or sets when the consent was given.
    /// </summary>
    public DateTime ConsentedAt { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which consent was given.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent (browser/device info).
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets when the consent was withdrawn (if applicable).
    /// </summary>
    public DateTime? WithdrawnAt { get; set; }

    /// <summary>
    /// Gets or sets the withdrawal reason.
    /// </summary>
    public string? WithdrawalReason { get; set; }

    /// <summary>
    /// Gets or sets when the consent expires (optional).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the consent method (e.g., "Web", "API", "Mobile").
    /// </summary>
    public string ConsentMethod { get; set; } = "Web";

    /// <summary>
    /// Gets or sets additional metadata (JSON).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Navigation property to the policy.
    /// </summary>
    public ConsentPolicy Policy { get; set; } = null!;

    /// <summary>
    /// Navigation property to the policy version.
    /// </summary>
    public PolicyVersion PolicyVersion { get; set; } = null!;

    /// <summary>
    /// Checks if the consent is currently valid.
    /// </summary>
    public bool IsValid()
    {
        if (!IsConsented || WithdrawnAt.HasValue)
            return false;

        if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow)
            return false;

        return true;
    }

    /// <summary>
    /// Withdraws the consent.
    /// </summary>
    public void Withdraw(string? reason = null)
    {
        WithdrawnAt = DateTime.UtcNow;
        WithdrawalReason = reason;
    }

    /// <summary>
    /// Checks if consent has expired.
    /// </summary>
    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    }
}
