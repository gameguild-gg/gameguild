namespace GameGuild.Social.Ratings;

/// <summary>
/// Thin facade that preserves the <see cref="IRatingService"/> contract for
/// existing consumers (controllers, GraphQL resolvers) while delegating all
/// work to focused sub-services.
/// </summary>
public class RatingService(
    IRatingCrudService crud,
    IRatingQueryService query,
    IRatingModerationService moderation) : IRatingService
{
    // ─── Core CRUD (delegates to IRatingCrudService) ─────────────────────────────

    public Task<Result<Rating>> RateAsync(
        Guid entityId, string entityType, int value,
        string? reviewText = null, string? reviewTitle = null,
        CancellationToken ct = default)
        => crud.RateAsync(entityId, entityType, value, reviewText, reviewTitle, ct);

    public Task<Result<Rating>> GetByIdAsync(Guid ratingId, CancellationToken ct = default)
        => crud.GetByIdAsync(ratingId, ct);

    public Task<Result<Rating>> GetUserRatingAsync(Guid entityId, string entityType, CancellationToken ct = default)
        => crud.GetUserRatingAsync(entityId, entityType, ct);

    public Task<Result> DeleteAsync(Guid ratingId, CancellationToken ct = default)
        => crud.DeleteAsync(ratingId, ct);

    // ─── Queries & Statistics (delegates to IRatingQueryService) ──────────────────

    public Task<Result<IEnumerable<Rating>>> GetRatingsAsync(
        Guid entityId, string entityType,
        int? minValue = null, int? maxValue = null,
        bool? withReviewOnly = null, bool? verifiedOnly = null,
        RatingSortOrder sortOrder = RatingSortOrder.MostRecent,
        int skip = 0, int take = 20, CancellationToken ct = default)
        => query.GetRatingsAsync(entityId, entityType, minValue, maxValue,
            withReviewOnly, verifiedOnly, sortOrder, skip, take, ct);

    public Task<Result<RatingSummary>> GetSummaryAsync(Guid entityId, string entityType, CancellationToken ct = default)
        => query.GetSummaryAsync(entityId, entityType, ct);

    public Task<Result<bool>> HasUserRatedAsync(Guid entityId, string entityType, CancellationToken ct = default)
        => query.HasUserRatedAsync(entityId, entityType, ct);

    public Task<Result<IEnumerable<Rating>>> GetUserRatingsAsync(
        Guid userId, string? entityType = null, int skip = 0, int take = 20, CancellationToken ct = default)
        => query.GetUserRatingsAsync(userId, entityType, skip, take, ct);

    public Task<Result<int>> GetCountAsync(Guid entityId, string entityType, CancellationToken ct = default)
        => query.GetCountAsync(entityId, entityType, ct);

    public Task<Result<Dictionary<Guid, RatingSummary>>> GetSummariesBatchAsync(
        IEnumerable<Guid> entityIds, string entityType, CancellationToken ct = default)
        => query.GetSummariesBatchAsync(entityIds, entityType, ct);

    public Task<Result<Dictionary<Guid, Rating>>> GetUserRatingsBatchAsync(
        IEnumerable<Guid> entityIds, string entityType, CancellationToken ct = default)
        => query.GetUserRatingsBatchAsync(entityIds, entityType, ct);

    public Task<Result> RecalculateSummaryAsync(Guid entityId, string entityType, CancellationToken ct = default)
        => query.RecalculateSummaryAsync(entityId, entityType, ct);

    public Task<Result<IEnumerable<RatingSummary>>> GetTopRatedAsync(
        string entityType, int minRatings = 5, int take = 10, CancellationToken ct = default)
        => query.GetTopRatedAsync(entityType, minRatings, take, ct);

    public Task<Result<IEnumerable<Rating>>> GetRecentReviewsAsync(
        string? entityType = null, int take = 20, CancellationToken ct = default)
        => query.GetRecentReviewsAsync(entityType, take, ct);

    // ─── Moderation & Interactions (delegates to IRatingModerationService) ────────

    public Task<Result> VoteHelpfulAsync(Guid ratingId, bool isHelpful, CancellationToken ct = default)
        => moderation.VoteHelpfulAsync(ratingId, isHelpful, ct);

    public Task<Result> RemoveHelpfulVoteAsync(Guid ratingId, CancellationToken ct = default)
        => moderation.RemoveHelpfulVoteAsync(ratingId, ct);

    public Task<Result> ReportAsync(Guid ratingId, string reason, CancellationToken ct = default)
        => moderation.ReportAsync(ratingId, reason, ct);

    public Task<Result<IEnumerable<Rating>>> GetPendingModerationAsync(
        int skip = 0, int take = 20, CancellationToken ct = default)
        => moderation.GetPendingModerationAsync(skip, take, ct);

    public Task<Result> ApproveAsync(Guid ratingId, CancellationToken ct = default)
        => moderation.ApproveAsync(ratingId, ct);

    public Task<Result> RejectAsync(Guid ratingId, CancellationToken ct = default)
        => moderation.RejectAsync(ratingId, ct);

    public Task<Result> AdminDeleteAsync(Guid ratingId, CancellationToken ct = default)
        => moderation.AdminDeleteAsync(ratingId, ct);
}

/// <summary>
/// Standard errors for the rating service
/// </summary>
public static class RatingErrors
{
    public static Error NotFound => Error.NotFound("Rating.NotFound", "Rating not found");
    public static Error VoteNotFound => Error.NotFound("Rating.VoteNotFound", "Helpful vote not found");
    public static Error CannotVoteOwnRating => Error.Failure("Rating.CannotVoteOwnRating", "You cannot vote on your own rating");
    public static Error AlreadyRated => Error.Failure("Rating.AlreadyRated", "You have already rated this item");
    public static Error InvalidValue => Error.Validation("Rating.InvalidValue", "Rating value must be between 1 and 5");
}
