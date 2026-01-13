
namespace GameGuild.Programs;

/// <summary> Interface for peer review services </summary>
public interface IPeerReviewService {
  /// <summary> Create a new peer review assignment </summary>
  Task<PeerReview> CreateReviewAsync(Guid reviewerId, Guid revieweeId, Guid contentId, Guid submissionId);

  /// <summary> Submit a peer review </summary>
  Task<PeerReview> SubmitReviewAsync(Guid reviewId, decimal score, decimal maxScore, string feedback, List<ReviewCriteria>? criteria = null);

  /// <summary> Get review by ID </summary>
  Task<PeerReview?> GetReviewByIdAsync(Guid reviewId);

  /// <summary> Get all reviews for a submission </summary>
  Task<IEnumerable<PeerReview>> GetSubmissionReviewsAsync(Guid submissionId);

  /// <summary> Get all reviews assigned to a reviewer </summary>
  Task<IEnumerable<PeerReview>> GetReviewerAssignmentsAsync(Guid reviewerId);

  /// <summary> Get all reviews received by a user </summary>
  Task<IEnumerable<PeerReview>> GetUserReceivedReviewsAsync(Guid revieweeId);

  /// <summary> Calculate consensus score for a submission </summary>
  Task<ConsensusResult> CalculateConsensusAsync(Guid submissionId);

  /// <summary> Detect review conflicts (high score variance) </summary>
  Task<IEnumerable<ReviewConflict>> DetectConflictsAsync(Guid submissionId);

  /// <summary> Assign reviews automatically based on criteria </summary>
  Task<IEnumerable<PeerReview>> AutoAssignReviewsAsync(Guid submissionId, int numberOfReviewers = 3);

  /// <summary> Reassign review to different reviewer </summary>
  Task<PeerReview> ReassignReviewAsync(Guid reviewId, Guid newReviewerId, string reason);

  /// <summary> Escalate review for moderation </summary>
  Task<PeerReview> EscalateReviewAsync(Guid reviewId, Guid moderatorId, string reason);

  /// <summary> Get review statistics for a content item </summary>
  Task<ReviewStatistics> GetReviewStatisticsAsync(Guid contentId);

  /// <summary> Get reviews requiring moderation </summary>
  Task<IEnumerable<PeerReview>> GetReviewsRequiringModerationAsync();

  /// <summary> Update review status (admin/moderator function) </summary>
  Task<PeerReview> UpdateReviewStatusAsync(Guid reviewId, ReviewStatus status, Guid? moderatorId = null);

  /// <summary> Get reviewer performance metrics </summary>
  Task<ReviewerMetrics> GetReviewerMetricsAsync(Guid reviewerId);
}