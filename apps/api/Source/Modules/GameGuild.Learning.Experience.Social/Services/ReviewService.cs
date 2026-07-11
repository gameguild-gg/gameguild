using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Experience.Social.Services;

/// <summary>
/// Service implementation for course review operations
/// </summary>
public class ReviewService : IReviewService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(
        IApplicationDbContext context,
        ILogger<ReviewService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<CourseReview>> CreateReviewAsync(
        Guid courseId,
        Guid userId,
        int rating,
        string? title = null,
        string? content = null,
        Guid? enrollmentId = null,
        CancellationToken cancellationToken = default)
    {
        var existingReview = await _context.Set<CourseReview>()
            .FirstOrDefaultAsync(r => r.CourseId == courseId && r.UserId == userId, cancellationToken).ConfigureAwait(false);

        if (existingReview != null)
        {
            return Result.Failure<CourseReview>(Error.Failure("Review.AlreadyExists", "You have already reviewed this course"));
        }

        var review = CourseReview.Create(courseId, userId, rating, title, content, enrollmentId);
        _context.Set<CourseReview>().Add(review);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Review created for course {CourseId} by user {UserId} with rating {Rating}", courseId, userId, rating);
        return Result.Success(review);
    }

    public async Task<Result<CourseReview>> GetReviewByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var review = await _context.Set<CourseReview>()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken).ConfigureAwait(false);

        if (review == null)
        {
            return Result.Failure<CourseReview>(Error.NotFound("Review.NotFound", $"Review with ID {id} not found"));
        }

        return Result.Success(review);
    }

    public async Task<Result<IEnumerable<CourseReview>>> GetCourseReviewsAsync(
        Guid courseId,
        int skip = 0,
        int take = 20,
        bool approvedOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<CourseReview>()
            .Where(r => r.CourseId == courseId);

        if (approvedOnly)
        {
            query = query.Where(r => r.IsApproved);
        }

        var reviews = await query
            .OrderByDescending(r => r.IsFeatured)
            .ThenByDescending(r => r.HelpfulCount)
            .ThenByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<CourseReview>>(reviews);
    }

    public async Task<Result<IEnumerable<CourseReview>>> GetUserReviewsAsync(
        Guid userId,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var reviews = await _context.Set<CourseReview>()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<IEnumerable<CourseReview>>(reviews);
    }

    public async Task<Result<CourseReview>> ApproveReviewAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _context.Set<CourseReview>()
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken).ConfigureAwait(false);

        if (review == null)
        {
            return Result.Failure<CourseReview>(Error.NotFound("Review.NotFound", $"Review with ID {reviewId} not found"));
        }

        review.Approve();
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Review {ReviewId} approved", reviewId);
        return Result.Success(review);
    }

    public async Task<Result<CourseReview>> FeatureReviewAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _context.Set<CourseReview>()
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken).ConfigureAwait(false);

        if (review == null)
        {
            return Result.Failure<CourseReview>(Error.NotFound("Review.NotFound", $"Review with ID {reviewId} not found"));
        }

        review.Feature();
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Review {ReviewId} featured", reviewId);
        return Result.Success(review);
    }

    public async Task<Result<CourseReview>> UpdateReviewModerationAsync(
        Guid reviewId,
        bool isApproved,
        bool isFeatured,
        CancellationToken cancellationToken = default)
    {
        var review = await _context.Set<CourseReview>()
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken).ConfigureAwait(false);

        if (review == null)
        {
            return Result.Failure<CourseReview>(Error.NotFound("Review.NotFound", $"Review with ID {reviewId} not found"));
        }

        review.SetModeration(isApproved, isFeatured);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Review {ReviewId} moderation updated: approved={IsApproved}, featured={IsFeatured}",
            reviewId,
            isApproved,
            isFeatured);
        return Result.Success(review);
    }

    public async Task<Result<CourseReview>> MarkReviewHelpfulAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _context.Set<CourseReview>()
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken).ConfigureAwait(false);

        if (review == null)
        {
            return Result.Failure<CourseReview>(Error.NotFound("Review.NotFound", $"Review with ID {reviewId} not found"));
        }

        review.MarkHelpful();
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(review);
    }

    public async Task<Result<bool>> DeleteReviewAsync(Guid reviewId, Guid userId, CancellationToken cancellationToken = default)
    {
        var review = await _context.Set<CourseReview>()
            .FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken).ConfigureAwait(false);

        if (review == null)
        {
            return Result.Failure<bool>(Error.NotFound("Review.NotFound", $"Review with ID {reviewId} not found"));
        }

        if (review.UserId != userId)
        {
            return Result.Failure<bool>(Error.Failure("Review.Unauthorized", "You can only delete your own reviews"));
        }

        _context.Set<CourseReview>().Remove(review);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Review {ReviewId} deleted by user {UserId}", reviewId, userId);
        return Result.Success(true);
    }

    public async Task<Result<CourseRatingStats>> GetCourseRatingStatsAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var reviews = await _context.Set<CourseReview>()
            .Where(r => r.CourseId == courseId && r.IsApproved)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (!reviews.Any())
        {
            return Result.Success(new CourseRatingStats(courseId, 0, 0, 0, 0, 0, 0, 0, 0));
        }

        var stats = new CourseRatingStats(
            courseId,
            reviews.Average(r => r.Rating),
            reviews.Count,
            reviews.Count(r => r.Rating == 5),
            reviews.Count(r => r.Rating == 4),
            reviews.Count(r => r.Rating == 3),
            reviews.Count(r => r.Rating == 2),
            reviews.Count(r => r.Rating == 1),
            reviews.Count(r => r.IsFeatured));

        return Result.Success(stats);
    }
}
