using GameGuild.Common;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Represents a peer review assignment for grading activities
/// </summary>
[Table("peer_reviews")]
[Index(nameof(ContentInteractionId))]
[Index(nameof(ReviewerId))]
[Index(nameof(RevieweeId))]
[Index(nameof(Status))]
[Index(nameof(AssignedAt))]
public class PeerReview : Entity {
  /// <summary>
  /// Foreign key to the content interaction being reviewed
  /// </summary>
  [Required]
  public Guid ContentInteractionId { get; set; }

  /// <summary>
  /// Navigation property to the content interaction
  /// </summary>
  [ForeignKey(nameof(ContentInteractionId))]
  public virtual ContentInteraction ContentInteraction { get; set; } = null!;

  /// <summary>
  /// Foreign key to the user doing the review
  /// </summary>
  [Required]
  public Guid ReviewerId { get; set; }

  /// <summary>
  /// Navigation property to the reviewer
  /// </summary>
  [ForeignKey(nameof(ReviewerId))]
  public virtual User Reviewer { get; set; } = null!;

  /// <summary>
  /// Foreign key to the user being reviewed
  /// </summary>
  [Required]
  public Guid RevieweeId { get; set; }

  /// <summary>
  /// Navigation property to the reviewee
  /// </summary>
  [ForeignKey(nameof(RevieweeId))]
  public virtual User Reviewee { get; set; } = null!;

  /// <summary>
  /// Current status of the peer review
  /// </summary>
  public PeerReviewStatus Status { get; set; } = PeerReviewStatus.Assigned;

  /// <summary>
  /// When the review was assigned
  /// </summary>
  public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// When the review was submitted
  /// </summary>
  public DateTime? SubmittedAt { get; set; }

  /// <summary>
  /// Due date for the review
  /// </summary>
  public DateTime? DueDate { get; set; }

  /// <summary>
  /// Grade given by the peer reviewer (0-100)
  /// </summary>
  [Range(0, 100)]
  public decimal? Grade { get; set; }

  /// <summary>
  /// Written feedback from the peer reviewer
  /// </summary>
  public string? Feedback { get; set; }

  /// <summary>
  /// Detailed review data (JSON)
  /// For storing rubric scores, detailed comments, etc.
  /// </summary>
  [Column(TypeName = "jsonb")]
  public string? ReviewData { get; set; }

  /// <summary>
  /// Whether the reviewee accepted this review
  /// </summary>
  public bool? IsAccepted { get; set; }

  /// <summary>
  /// Reason for acceptance/rejection
  /// </summary>
  public string? AcceptanceReason { get; set; }

  /// <summary>
  /// When the reviewee responded to the review
  /// </summary>
  public DateTime? ResponseAt { get; set; }

  /// <summary>
  /// Quality rating of this review (used for reviewer reputation)
  /// </summary>
  [Range(1, 5)]
  public int? ReviewQuality { get; set; }

  /// <summary>
  /// Submit the peer review
  /// </summary>
  public void SubmitReview(decimal grade, string? feedback = null, string? reviewData = null) {
    Status = PeerReviewStatus.Submitted;
    Grade = grade;
    Feedback = feedback;
    ReviewData = reviewData;
    SubmittedAt = DateTime.UtcNow;
    Touch();
  }

  /// <summary>
  /// Accept the peer review
  /// </summary>
  public void AcceptReview(string? reason = null) {
    IsAccepted = true;
    AcceptanceReason = reason;
    ResponseAt = DateTime.UtcNow;
    Status = PeerReviewStatus.Accepted;
    Touch();
  }

  /// <summary>
  /// Reject the peer review
  /// </summary>
  public void RejectReview(string reason) {
    IsAccepted = false;
    AcceptanceReason = reason;
    ResponseAt = DateTime.UtcNow;
    Status = PeerReviewStatus.Rejected;
    Touch();
  }

  /// <summary>
  /// Rate the quality of this review
  /// </summary>
  public void RateReview(int quality) {
    ReviewQuality = Math.Max(1, Math.Min(5, quality));
    Touch();
  }
}

/// <summary>
/// Peer review status enumeration
/// </summary>
public enum PeerReviewStatus {
  Assigned = 0,
  InProgress = 1,
  Submitted = 2,
  Accepted = 3,
  Rejected = 4,
  Disputed = 5,
  Expired = 6,
  Cancelled = 7,
}
