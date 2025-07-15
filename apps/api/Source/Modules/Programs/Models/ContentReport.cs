using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Represents a report submitted by a user for content or behavior issues
/// </summary>
[Table("content_reports")]
[Index(nameof(ReporterId))]
[Index(nameof(ReportType))]
[Index(nameof(Status))]
[Index(nameof(CreatedAt))]
public class ContentReport : Entity
{
    /// <summary>
    /// Foreign key to the user who submitted the report
    /// </summary>
    [Required]
    public Guid ReporterId { get; set; }

    /// <summary>
    /// Navigation property to the reporter
    /// </summary>
    [ForeignKey(nameof(ReporterId))]
    public virtual User Reporter { get; set; } = null!;

    /// <summary>
    /// Type of report
    /// </summary>
    public ReportType ReportType { get; set; }

    /// <summary>
    /// Subject of the report
    /// </summary>
    public ReportSubject Subject { get; set; }

    /// <summary>
    /// ID of the reported content (if applicable)
    /// </summary>
    public Guid? ContentId { get; set; }

    /// <summary>
    /// ID of the reported user (if applicable)
    /// </summary>
    public Guid? ReportedUserId { get; set; }

    /// <summary>
    /// Navigation property to the reported user
    /// </summary>
    [ForeignKey(nameof(ReportedUserId))]
    public virtual User? ReportedUser { get; set; }

    /// <summary>
    /// ID of the peer review being reported (if applicable)
    /// </summary>
    public Guid? PeerReviewId { get; set; }

    /// <summary>
    /// Navigation property to the peer review
    /// </summary>
    [ForeignKey(nameof(PeerReviewId))]
    public virtual PeerReview? PeerReview { get; set; }

    /// <summary>
    /// Report category
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the issue
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Evidence or additional details (JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Evidence { get; set; }

    /// <summary>
    /// Current status of the report
    /// </summary>
    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    /// <summary>
    /// Priority level of the report
    /// </summary>
    public ReportPriority Priority { get; set; } = ReportPriority.Medium;

    /// <summary>
    /// ID of the moderator handling the report
    /// </summary>
    public Guid? ModeratorId { get; set; }

    /// <summary>
    /// Navigation property to the moderator
    /// </summary>
    [ForeignKey(nameof(ModeratorId))]
    public virtual User? Moderator { get; set; }

    /// <summary>
    /// When the report was assigned to a moderator
    /// </summary>
    public DateTime? AssignedAt { get; set; }

    /// <summary>
    /// When the report was resolved
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Resolution notes from the moderator
    /// </summary>
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Action taken as a result of the report
    /// </summary>
    public string? ActionTaken { get; set; }

    /// <summary>
    /// Assign report to a moderator
    /// </summary>
    public void AssignToModerator(Guid moderatorId)
    {
        ModeratorId = moderatorId;
        AssignedAt = DateTime.UtcNow;
        Status = ReportStatus.InReview;
        Touch();
    }

    /// <summary>
    /// Resolve the report
    /// </summary>
    public void Resolve(string resolutionNotes, string? actionTaken = null)
    {
        Status = ReportStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        ResolutionNotes = resolutionNotes;
        ActionTaken = actionTaken;
        Touch();
    }

    /// <summary>
    /// Dismiss the report
    /// </summary>
    public void Dismiss(string reason)
    {
        Status = ReportStatus.Dismissed;
        ResolvedAt = DateTime.UtcNow;
        ResolutionNotes = reason;
        Touch();
    }
}

/// <summary>
/// Type of report
/// </summary>
public enum ReportType
{
    Content = 0,
    Behavior = 1,
    PeerReview = 2,
    Technical = 3,
    Other = 4
}

/// <summary>
/// Subject being reported
/// </summary>
public enum ReportSubject
{
    Content = 0,
    User = 1,
    PeerReview = 2,
    System = 3
}

/// <summary>
/// Report status
/// </summary>
public enum ReportStatus
{
    Pending = 0,
    InReview = 1,
    Resolved = 2,
    Dismissed = 3,
    Escalated = 4
}

/// <summary>
/// Report priority
/// </summary>
public enum ReportPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
