namespace GameGuild.Social.Ratings;

/// <summary>
/// Handles review interactions (helpful votes, reports) and admin moderation.
/// </summary>
public interface IRatingModerationService
{
    /// <summary>Mark a review as helpful or not helpful</summary>
    Task<Result> VoteHelpfulAsync(Guid ratingId, bool isHelpful, CancellationToken ct = default);

    /// <summary>Remove helpful vote</summary>
    Task<Result> RemoveHelpfulVoteAsync(Guid ratingId, CancellationToken ct = default);

    /// <summary>Report a review for moderation</summary>
    Task<Result> ReportAsync(Guid ratingId, string reason, CancellationToken ct = default);

    /// <summary>Get ratings pending moderation</summary>
    Task<Result<IEnumerable<Rating>>> GetPendingModerationAsync(
        int skip = 0,
        int take = 20,
        CancellationToken ct = default);

    /// <summary>Approve a rating</summary>
    Task<Result> ApproveAsync(Guid ratingId, CancellationToken ct = default);

    /// <summary>Reject a rating</summary>
    Task<Result> RejectAsync(Guid ratingId, CancellationToken ct = default);

    /// <summary>Admin delete any rating</summary>
    Task<Result> AdminDeleteAsync(Guid ratingId, CancellationToken ct = default);
}
