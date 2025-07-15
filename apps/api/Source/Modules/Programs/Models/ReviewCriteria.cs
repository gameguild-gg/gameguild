using System.ComponentModel.DataAnnotations;
using GameGuild.Common;
using GameGuild.Common.Models;

namespace GameGuild.Modules.Programs.Models;

/// <summary>
/// Criteria for peer review evaluation
/// </summary>
public class ReviewCriteria : BaseEntity
{
    /// <summary>
    /// Name of the criteria (e.g., "Code Quality", "Documentation", "Performance")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this criteria evaluates
    /// </summary>
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Weight of this criteria in overall score (0.0 to 1.0)
    /// </summary>
    public decimal Weight { get; set; } = 1.0m;

    /// <summary>
    /// Maximum score for this criteria
    /// </summary>
    public decimal MaxScore { get; set; } = 10.0m;

    /// <summary>
    /// The score given for this criteria
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// Specific feedback for this criteria
    /// </summary>
    [MaxLength(1000)]
    public string Feedback { get; set; } = string.Empty;

    /// <summary>
    /// Whether this criteria is required for evaluation
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Order in which this criteria appears
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// The peer review this criteria belongs to
    /// </summary>
    public Guid PeerReviewId { get; set; }
    public virtual PeerReview PeerReview { get; set; } = null!;
}

/// <summary>
/// Status of a review
/// </summary>
public enum ReviewStatus
{
    Pending = 0,
    InProgress = 1,
    Submitted = 2,
    Approved = 3,
    Rejected = 4,
    RequiresRevision = 5,
    Escalated = 6
}
