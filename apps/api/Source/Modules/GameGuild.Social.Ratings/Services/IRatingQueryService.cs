namespace GameGuild.Social.Ratings;

/// <summary>
/// Handles rating queries, batch lookups, aggregation, and statistics.
/// </summary>
public interface IRatingQueryService
{
    /// <summary>Get all ratings for an entity with filtering and sorting</summary>
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
    Task<Result<IEnumerable<Rating>>> GetUserRatingsAsync(
        Guid userId,
        string? entityType = null,
        int skip = 0,
        int take = 20,
        CancellationToken ct = default);

    /// <summary>Get ratings count for an entity</summary>
    Task<Result<int>> GetCountAsync(Guid entityId, string entityType, CancellationToken ct = default);

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
