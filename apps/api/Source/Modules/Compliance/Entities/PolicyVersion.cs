using GameGuild.Core.Domain;

namespace GameGuild.Modules.Compliance.Entities;

/// <summary>
/// Represents a version of a consent policy.
/// </summary>
public sealed class PolicyVersion : EntityBase
{
    /// <summary>
    /// Gets or sets the policy ID this version belongs to.
    /// </summary>
    public Guid PolicyId { get; set; }

    /// <summary>
    /// Gets or sets the version number (e.g., "1.0", "2.1").
    /// </summary>
    public string VersionNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the policy content (HTML/Markdown).
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type (HTML, Markdown, PlainText).
    /// </summary>
    public ContentType ContentType { get; set; } = ContentType.HTML;

    /// <summary>
    /// Gets or sets a summary of changes in this version.
    /// </summary>
    public string? ChangeLog { get; set; }

    /// <summary>
    /// Gets or sets the effective date when this version becomes active.
    /// </summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Gets or sets when this version expires (optional).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets whether this is the current active version.
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Gets or sets the author/creator of this version.
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Navigation property to the policy.
    /// </summary>
    public ConsentPolicy Policy { get; set; } = null!;

    /// <summary>
    /// Navigation property for user consents to this specific version.
    /// </summary>
    public ICollection<UserConsent> UserConsents { get; set; } = new List<UserConsent>();

    /// <summary>
    /// Checks if this version is currently active.
    /// </summary>
    public bool IsActive()
    {
        var now = DateTime.UtcNow;
        return IsCurrent &&
               EffectiveDate <= now &&
               (!ExpiresAt.HasValue || ExpiresAt.Value > now);
    }

    /// <summary>
    /// Marks this version as current.
    /// </summary>
    public void MakeCurrent()
    {
        IsCurrent = true;
    }

    /// <summary>
    /// Archives this version (marks as no longer current).
    /// </summary>
    public void Archive()
    {
        IsCurrent = false;
    }
}

/// <summary>
/// Content types for policy versions.
/// </summary>
public enum ContentType
{
    PlainText = 1,
    HTML = 2,
    Markdown = 3
}
