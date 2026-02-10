using Microsoft.EntityFrameworkCore;

namespace GameGuild.Assets;

/// <summary>
/// User report of inappropriate content.
/// </summary>
[Table("asset_reports")]
[Index(nameof(AssetReferenceId))]
[Index(nameof(ReportedByUserId))]
[Index(nameof(Status))]
public class AssetReport : EntityBase
{
    /// <summary>
    /// Default constructor for EF Core.
    /// </summary>
    protected AssetReport() { }

    /// <summary>
    /// Creates a new asset report.
    /// </summary>
    public AssetReport(
        Guid assetReferenceId,
        Guid reportedByUserId,
        ReportReason reason,
        string? details)
    {
        AssetReferenceId = assetReferenceId;
        ReportedByUserId = reportedByUserId;
        Reason = reason;
        Details = details;
    }

    /// <summary>
    /// Reported asset reference.
    /// </summary>
    public Guid AssetReferenceId { get; set; }

    /// <summary>
    /// User who submitted the report.
    /// </summary>
    public Guid ReportedByUserId { get; set; }

    /// <summary>
    /// Report reason category.
    /// </summary>
    public ReportReason Reason { get; set; }

    /// <summary>
    /// Additional details provided by the reporter.
    /// </summary>
    [MaxLength(2000)]
    public string? Details { get; set; }

    /// <summary>
    /// Review status.
    /// </summary>
    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    /// <summary>
    /// Moderator who reviewed.
    /// </summary>
    public Guid? ReviewedByUserId { get; set; }

    /// <summary>
    /// Review decision.
    /// </summary>
    public ReviewDecision? Decision { get; set; }

    /// <summary>
    /// Review notes.
    /// </summary>
    [MaxLength(2000)]
    public string? ReviewNotes { get; set; }

    /// <summary>
    /// When reviewed.
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    // Navigation

    [ForeignKey(nameof(AssetReferenceId))]
    public virtual AssetReference Reference { get; set; } = null!;

    /// <summary>
    /// Returns true if the report is pending review.
    /// </summary>
    [NotMapped]
    public bool IsPending => Status == ReportStatus.Pending || Status == ReportStatus.UnderReview;

    /// <summary>
    /// Submits a review for this report.
    /// </summary>
    public void SubmitReview(Guid reviewerId, ReviewDecision decision, string? notes = null)
    {
        ReviewedByUserId = reviewerId;
        Decision = decision;
        ReviewNotes = notes;
        ReviewedAt = SystemClock.UtcNow;
        Status = decision == ReviewDecision.NoAction ? ReportStatus.Dismissed : ReportStatus.Resolved;
    }
}
