namespace GameGuild.Learning.Experience.Social.Services;

/// <summary>
/// Service interface for course review operations
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Creates a new course review
    /// </summary>
    Task<Result<CourseReview>> CreateReviewAsync(
        Guid courseId,
        Guid userId,
        int rating,
        string? title = null,
        string? content = null,
        Guid? enrollmentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a review by ID
    /// </summary>
    Task<Result<CourseReview>> GetReviewByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reviews for a course
    /// </summary>
    Task<Result<IEnumerable<CourseReview>>> GetCourseReviewsAsync(
        Guid courseId,
        int skip = 0,
        int take = 20,
        bool approvedOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reviews by a user
    /// </summary>
    Task<Result<IEnumerable<CourseReview>>> GetUserReviewsAsync(
        Guid userId,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a review (admin action)
    /// </summary>
    Task<Result<CourseReview>> ApproveReviewAsync(Guid reviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Features a review (admin action)
    /// </summary>
    Task<Result<CourseReview>> FeatureReviewAsync(Guid reviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets both moderation flags, allowing administrators to reverse approval and featured state.
    /// </summary>
    Task<Result<CourseReview>> UpdateReviewModerationAsync(
        Guid reviewId,
        bool isApproved,
        bool isFeatured,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a review as helpful
    /// </summary>
    Task<Result<CourseReview>> MarkReviewHelpfulAsync(Guid reviewId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a review
    /// </summary>
    Task<Result<bool>> DeleteReviewAsync(Guid reviewId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets average rating for a course
    /// </summary>
    Task<Result<CourseRatingStats>> GetCourseRatingStatsAsync(Guid courseId, CancellationToken cancellationToken = default);
}
