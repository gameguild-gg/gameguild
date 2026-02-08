
namespace GameGuild.Social.Ratings;

/// <summary>
/// Service interface for managing polymorphic ratings across the system
/// </summary>
public interface IRatingService
{
    // ─── Core CRUD Operations ────────────────────────────────────────────────────
    
    /// <summary>Create or update a rating for an entity</summary>
    Task<Result<Rating>> RateAsync(
        Guid entityId, 
        string entityType, 
        int value, 
        string? reviewText = null, 
        string? reviewTitle = null,
        CancellationToken ct = default);

    /// <summary>Get a specific rating by ID</summary>
    Task<Result<Rating>> GetByIdAsync(Guid ratingId, CancellationToken ct = default);

    /// <summary>Get the current user's rating for an entity</summary>
    Task<Result<Rating>> GetUserRatingAsync(Guid entityId, string entityType, CancellationToken ct = default);

    /// <summary>Delete the current user's rating</summary>
    Task<Result> DeleteAsync(Guid ratingId, CancellationToken ct = default);

    // ─── Query Operations ────────────────────────────────────────────────────────

    /// <summary>Get all ratings for an entity</summary>
    Task<Result<IEnumerable<Rating>>> GetRatingsAsync(
        Guid entityId, 
        string entityType, 
        int? minValue = null,
        int? maxValue = null,
        bool? withReviewOnly = null,
        bool? verifiedOnly = null,
        RatingSortOrder sortOrder = RatingSortOrder.MostRecent,
        int skip = 0, 
        int take = 20,
        CancellationToken ct = default);

    /// <summary>Get rating summary (aggregate stats) for an entity</summary>
    Task<Result<RatingSummary>> GetSummaryAsync(Guid entityId, string entityType, CancellationToken ct = default);

    /// <summary>Check if the current user has rated an entity</summary>
    Task<Result<bool>> HasUserRatedAsync(Guid entityId, string entityType, CancellationToken ct = default);

    /// <summary>Get all ratings by a specific user</summary>
    Task<Result<IEnumerable<Rating>>> GetUserRatingsAsync(Guid userId, string? entityType = null, int skip = 0, int take = 20, CancellationToken ct = default);

    /// <summary>Get ratings count for an entity</summary>
    Task<Result<int>> GetCountAsync(Guid entityId, string entityType, CancellationToken ct = default);

    // ─── Batch Operations ────────────────────────────────────────────────────────

    /// <summary>Get summaries for multiple entities of the same type</summary>
    Task<Result<Dictionary<Guid, RatingSummary>>> GetSummariesBatchAsync(
        IEnumerable<Guid> entityIds, 
        string entityType, 
        CancellationToken ct = default);

    /// <summary>Get current user's ratings for multiple entities</summary>
    Task<Result<Dictionary<Guid, Rating>>> GetUserRatingsBatchAsync(
        IEnumerable<Guid> entityIds, 
        string entityType, 
        CancellationToken ct = default);

    // ─── Review Interactions ─────────────────────────────────────────────────────

    /// <summary>Mark a review as helpful or not helpful</summary>
    Task<Result> VoteHelpfulAsync(Guid ratingId, bool isHelpful, CancellationToken ct = default);

    /// <summary>Remove helpful vote</summary>
    Task<Result> RemoveHelpfulVoteAsync(Guid ratingId, CancellationToken ct = default);

    /// <summary>Report a review for moderation</summary>
    Task<Result> ReportAsync(Guid ratingId, string reason, CancellationToken ct = default);

    // ─── Moderation (Admin) ──────────────────────────────────────────────────────

    /// <summary>Get ratings pending moderation</summary>
    Task<Result<IEnumerable<Rating>>> GetPendingModerationAsync(int skip = 0, int take = 20, CancellationToken ct = default);

    /// <summary>Approve a rating</summary>
    Task<Result> ApproveAsync(Guid ratingId, CancellationToken ct = default);

    /// <summary>Reject a rating</summary>
    Task<Result> RejectAsync(Guid ratingId, CancellationToken ct = default);

    /// <summary>Admin delete any rating</summary>
    Task<Result> AdminDeleteAsync(Guid ratingId, CancellationToken ct = default);

    // ─── Statistics & Analytics ──────────────────────────────────────────────────

    /// <summary>Recalculate rating summary for an entity</summary>
    Task<Result> RecalculateSummaryAsync(Guid entityId, string entityType, CancellationToken ct = default);

    /// <summary>Get top-rated entities of a type</summary>
    Task<Result<IEnumerable<RatingSummary>>> GetTopRatedAsync(
        string entityType, 
        int minRatings = 5, 
        int take = 10, 
        CancellationToken ct = default);

    /// <summary>Get recently reviewed entities</summary>
    Task<Result<IEnumerable<Rating>>> GetRecentReviewsAsync(
        string? entityType = null, 
        int take = 20, 
        CancellationToken ct = default);
}

/// <summary>
/// Sort order options for rating queries
/// </summary>
public enum RatingSortOrder
{
    MostRecent,
    Oldest,
    HighestRating,
    LowestRating,
    MostHelpful
}
