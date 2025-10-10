using GameGuild.Core.Entities;

namespace GameGuild.Modules.Compliance.Entities;

/// <summary>
/// Represents a consent policy (e.g., Privacy Policy, Terms of Service, Cookie Policy).
/// </summary>
public sealed class ConsentPolicy : EntityBase
{
    /// <summary>
    /// Gets or sets the tenant ID for multi-tenant support.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the policy name (e.g., "Privacy Policy", "Terms of Service").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the policy type.
    /// </summary>
    public PolicyType Type { get; set; }

    /// <summary>
    /// Gets or sets the policy description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this policy is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets whether user consent is required for this policy.
    /// </summary>
    public bool RequiresConsent { get; set; }

    /// <summary>
    /// Gets or sets the current version ID.
    /// </summary>
    public Guid? CurrentVersionId { get; set; }

    /// <summary>
    /// Gets or sets when this policy was published.
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Navigation property for policy versions.
    /// </summary>
    public ICollection<PolicyVersion> Versions { get; set; } = new List<PolicyVersion>();

    /// <summary>
    /// Navigation property for user consents.
    /// </summary>
    public ICollection<UserConsent> UserConsents { get; set; } = new List<UserConsent>();

    /// <summary>
    /// Navigation property for current version.
    /// </summary>
    public PolicyVersion? CurrentVersion { get; set; }

    /// <summary>
    /// Publishes the policy with a specific version.
    /// </summary>
    public void Publish(Guid versionId)
    {
        CurrentVersionId = versionId;
        PublishedAt = DateTime.UtcNow;
        IsActive = true;
    }

    /// <summary>
    /// Deactivates the policy.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Checks if the policy requires consent.
    /// </summary>
    public bool NeedsUserConsent() => RequiresConsent && IsActive;
}

/// <summary>
/// Policy types.
/// </summary>
public enum PolicyType
{
    PrivacyPolicy = 1,
    TermsOfService = 2,
    CookiePolicy = 3,
    DataProcessingAgreement = 4,
    AcceptableUsePolicy = 5,
    GDPR = 6,
    CCPA = 7,
    COPPA = 8,
    Custom = 99
}
