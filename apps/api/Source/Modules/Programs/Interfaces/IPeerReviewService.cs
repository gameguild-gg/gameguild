namespace GameGuild.Modules.Programs;

/// <summary>
/// Interface for peer review services
/// </summary>
public interface IPeerReviewService {
  /// <summary>
  /// Create a new peer review assignment
  /// </summary>
  Task<PeerReview> CreateReviewAsync(Guid reviewerId, Guid revieweeId, Guid contentId, Guid submissionId);

  /// <summary>
  /// Submit a peer review
  /// </summary>
  Task<PeerReview> SubmitReviewAsync(Guid reviewId, decimal score, decimal maxScore, string feedback, List<ReviewCriteria>? criteria = null);

  /// <summary>
  /// Get review by ID
  /// </summary>
  Task<PeerReview?> GetReviewByIdAsync(Guid reviewId);

  /// <summary>
  /// Get all reviews for a submission
  /// </summary>
  Task<IEnumerable<PeerReview>> GetSubmissionReviewsAsync(Guid submissionId);

  /// <summary>
  /// Get all reviews assigned to a reviewer
  /// </summary>
  Task<IEnumerable<PeerReview>> GetReviewerAssignmentsAsync(Guid reviewerId);

  /// <summary>
  /// Get all reviews received by a user
  /// </summary>
  Task<IEnumerable<PeerReview>> GetUserReceivedReviewsAsync(Guid revieweeId);

  /// <summary>
  /// Calculate consensus score for a submission
  /// </summary>
  Task<ConsensusResult> CalculateConsensusAsync(Guid submissionId);

  /// <summary>
  /// Detect review conflicts (high score variance)
  /// </summary>
  Task<IEnumerable<ReviewConflict>> DetectConflictsAsync(Guid submissionId);

  /// <summary>
  /// Assign reviews automatically based on criteria
  /// </summary>
  Task<IEnumerable<PeerReview>> AutoAssignReviewsAsync(Guid submissionId, int numberOfReviewers = 3);

  /// <summary>
  /// Reassign review to different reviewer
  /// </summary>
  Task<PeerReview> ReassignReviewAsync(Guid reviewId, Guid newReviewerId, string reason);

  /// <summary>
  /// Escalate review for moderation
  /// </summary>
  Task<PeerReview> EscalateReviewAsync(Guid reviewId, Guid moderatorId, string reason);

  /// <summary>
  /// Get review statistics for a content item
  /// </summary>
  Task<ReviewStatistics> GetReviewStatisticsAsync(Guid contentId);

  /// <summary>
  /// Get reviews requiring moderation
  /// </summary>
  Task<IEnumerable<PeerReview>> GetReviewsRequiringModerationAsync();

  /// <summary>
  /// Update review status (admin/moderator function)
  /// </summary>
  Task<PeerReview> UpdateReviewStatusAsync(Guid reviewId, ReviewStatus status, Guid? moderatorId = null);

  /// <summary>
  /// Get reviewer performance metrics
  /// </summary>
  Task<ReviewerMetrics> GetReviewerMetricsAsync(Guid reviewerId);
}

/// <summary>
/// Consensus calculation result
/// </summary>
public class ConsensusResult {
  public decimal ConsensusScore { get; set; }

  public decimal AverageScore { get; set; }

  public decimal ScoreVariance { get; set; }

  public int TotalReviews { get; set; }

  public int CompletedReviews { get; set; }

  public bool HasConsensus { get; set; }

  public decimal ConfidenceLevel { get; set; }
}

/// <summary>
/// Review conflict detection result
/// </summary>
public class ReviewConflict {
  public Guid SubmissionId { get; set; }

  public IList<PeerReview> ConflictingReviews { get; set; } = new List<PeerReview>();

  public decimal ScoreVariance { get; set; }

  public ConflictSeverity Severity { get; set; }

  public string Reason { get; set; } = string.Empty;

  public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Review statistics
/// </summary>
public class ReviewStatistics {
  public int TotalAssignments { get; set; }

  public int CompletedReviews { get; set; }

  public int PendingReviews { get; set; }

  public int EscalatedReviews { get; set; }

  public decimal AverageScore { get; set; }

  public decimal AverageCompletionTimeHours { get; set; }

  public Dictionary<ReviewStatus, int> ReviewsByStatus { get; set; } = new();

  public decimal ConsensusRate { get; set; }
}

/// <summary>
/// Reviewer performance metrics
/// </summary>
public class ReviewerMetrics {
  public Guid ReviewerId { get; set; }

  public int TotalReviewsAssigned { get; set; }

  public int TotalReviewsCompleted { get; set; }

  public decimal CompletionRate { get; set; }

  public decimal AverageScore { get; set; }

  public decimal AverageCompletionTimeHours { get; set; }

  public int ReviewsEscalated { get; set; }

  public int ReviewsInConflict { get; set; }

  public decimal ReliabilityScore { get; set; }

  public DateTime LastReviewDate { get; set; }
}

/// <summary>
/// Conflict severity levels
/// </summary>
public enum ConflictSeverity {
  Low = 0,
  Medium = 1,
  High = 2,
  Critical = 3,
}
