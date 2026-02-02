using GameGuild.Identity.Context.Actors;
using GameGuild.Models;
using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Social.Ratings;

/// <summary>
/// Implementation of the polymorphic rating service
/// </summary>
public class RatingService : IRatingService
{
    private readonly IApplicationDbContext _db;
    private readonly IActorContextAccessor _actorContextAccessor;
    private readonly ILogger<RatingService> _logger;

    public RatingService(
        IApplicationDbContext db,
        IActorContextAccessor actorContextAccessor,
        ILogger<RatingService> logger)
    {
        _db = db;
        _actorContextAccessor = actorContextAccessor;
        _logger = logger;
    }

    private Guid GetCurrentUserId() => _actorContextAccessor.ActorContext.SubjectIdAsGuid;

    // ─── Core CRUD Operations ────────────────────────────────────────────────────

    public async Task<Result<Rating>> RateAsync(
        Guid entityId,
        string entityType,
        int value,
        string? reviewText = null,
        string? reviewTitle = null,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        
        // Check if user already rated this entity
        var existingRating = await _db.Set<Rating>()
            .FirstOrDefaultAsync(r => 
                r.EntityId == entityId && 
                r.EntityType == entityType && 
                r.UserId == userId && 
                !r.IsDeleted, ct);

        if (existingRating != null)
        {
            // Update existing rating
            existingRating.Update(value, reviewText, reviewTitle);
            await _db.SaveChangesAsync(ct);
            
            _logger.LogInformation("User {UserId} updated rating for {EntityType}:{EntityId}", userId, entityType, entityId);
            
            // Trigger summary recalculation
            await RecalculateSummaryAsync(entityId, entityType, ct);
            
            return Result.Success(existingRating);
        }

        // Create new rating
        var rating = Rating.Create(userId, entityId, entityType, value, reviewText, reviewTitle);
        _db.Set<Rating>().Add(rating);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} created rating for {EntityType}:{EntityId} with value {Value}", 
            userId, entityType, entityId, value);

        // Trigger summary recalculation
        await RecalculateSummaryAsync(entityId, entityType, ct);

        return Result.Success(rating);
    }

    public async Task<Result<Rating>> GetByIdAsync(Guid ratingId, CancellationToken ct = default)
    {
        var rating = await _db.Set<Rating>()
            .FirstOrDefaultAsync(r => r.Id == ratingId && !r.IsDeleted, ct);

        if (rating == null)
            return Result.Failure<Rating>(RatingErrors.NotFound);

        return Result.Success(rating);
    }

    public async Task<Result<Rating>> GetUserRatingAsync(Guid entityId, string entityType, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        
        var rating = await _db.Set<Rating>()
            .FirstOrDefaultAsync(r => 
                r.EntityId == entityId && 
                r.EntityType == entityType && 
                r.UserId == userId && 
                !r.IsDeleted, ct);

        if (rating == null)
            return Result.Failure<Rating>(RatingErrors.NotFound);

        return Result.Success(rating);
    }

    public async Task<Result> DeleteAsync(Guid ratingId, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        
        var rating = await _db.Set<Rating>()
            .FirstOrDefaultAsync(r => r.Id == ratingId && r.UserId == userId && !r.IsDeleted, ct);

        if (rating == null)
            return Result.Failure(RatingErrors.NotFound);

        rating.IsDeleted = true;
        rating.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} deleted rating {RatingId}", userId, ratingId);

        // Trigger summary recalculation
        await RecalculateSummaryAsync(rating.EntityId, rating.EntityType, ct);

        return Result.Success();
    }

    // ─── Query Operations ────────────────────────────────────────────────────────

    public async Task<Result<IEnumerable<Rating>>> GetRatingsAsync(
        Guid entityId,
        string entityType,
        int? minValue = null,
        int? maxValue = null,
        bool? withReviewOnly = null,
        bool? verifiedOnly = null,
        RatingSortOrder sortOrder = RatingSortOrder.MostRecent,
        int skip = 0,
        int take = 20,
        CancellationToken ct = default)
    {
        var query = _db.Set<Rating>()
            .Where(r => r.EntityId == entityId && r.EntityType == entityType && !r.IsDeleted)
            .Where(r => r.ModerationStatus == RatingModerationStatus.Approved);

        if (minValue.HasValue)
            query = query.Where(r => r.Value >= minValue.Value);
        
        if (maxValue.HasValue)
            query = query.Where(r => r.Value <= maxValue.Value);

        if (withReviewOnly == true)
            query = query.Where(r => r.ReviewText != null && r.ReviewText != "");

        if (verifiedOnly == true)
            query = query.Where(r => r.IsVerified);

        query = sortOrder switch
        {
            RatingSortOrder.Oldest => query.OrderBy(r => r.CreatedAt),
            RatingSortOrder.HighestRating => query.OrderByDescending(r => r.Value).ThenByDescending(r => r.CreatedAt),
            RatingSortOrder.LowestRating => query.OrderBy(r => r.Value).ThenByDescending(r => r.CreatedAt),
            RatingSortOrder.MostHelpful => query.OrderByDescending(r => r.HelpfulCount).ThenByDescending(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        var ratings = await query.Skip(skip).Take(take).ToListAsync(ct);
        return Result.Success<IEnumerable<Rating>>(ratings);
    }

    public async Task<Result<RatingSummary>> GetSummaryAsync(Guid entityId, string entityType, CancellationToken ct = default)
    {
        var summary = await _db.Set<RatingSummary>()
            .FirstOrDefaultAsync(s => s.EntityId == entityId && s.EntityType == entityType && !s.IsDeleted, ct);

        if (summary == null)
        {
            // Create initial summary on-demand
            summary = RatingSummary.Create(entityId, entityType);
            _db.Set<RatingSummary>().Add(summary);
            
            // Calculate from existing ratings
            var ratings = await _db.Set<Rating>()
                .Where(r => r.EntityId == entityId && r.EntityType == entityType && !r.IsDeleted)
                .ToListAsync(ct);
            
            summary.Recalculate(ratings);
            await _db.SaveChangesAsync(ct);
        }

        return Result.Success(summary);
    }

    public async Task<Result<bool>> HasUserRatedAsync(Guid entityId, string entityType, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        
        var hasRated = await _db.Set<Rating>()
            .AnyAsync(r => 
                r.EntityId == entityId && 
                r.EntityType == entityType && 
                r.UserId == userId && 
                !r.IsDeleted, ct);

        return Result.Success(hasRated);
    }

    public async Task<Result<IEnumerable<Rating>>> GetUserRatingsAsync(
        Guid userId, 
        string? entityType = null, 
        int skip = 0, 
        int take = 20, 
        CancellationToken ct = default)
    {
        var query = _db.Set<Rating>()
            .Where(r => r.UserId == userId && !r.IsDeleted)
            .Where(r => r.ModerationStatus == RatingModerationStatus.Approved);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(r => r.EntityType == entityType);

        var ratings = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return Result.Success<IEnumerable<Rating>>(ratings);
    }

    public async Task<Result<int>> GetCountAsync(Guid entityId, string entityType, CancellationToken ct = default)
    {
        var count = await _db.Set<Rating>()
            .CountAsync(r => 
                r.EntityId == entityId && 
                r.EntityType == entityType && 
                !r.IsDeleted &&
                r.ModerationStatus == RatingModerationStatus.Approved, ct);

        return Result.Success(count);
    }

    // ─── Batch Operations ────────────────────────────────────────────────────────

    public async Task<Result<Dictionary<Guid, RatingSummary>>> GetSummariesBatchAsync(
        IEnumerable<Guid> entityIds,
        string entityType,
        CancellationToken ct = default)
    {
        var ids = entityIds.ToList();
        
        var summaries = await _db.Set<RatingSummary>()
            .Where(s => ids.Contains(s.EntityId) && s.EntityType == entityType && !s.IsDeleted)
            .ToListAsync(ct);

        var result = summaries.ToDictionary(s => s.EntityId, s => s);

        // Create empty summaries for entities without ratings
        foreach (var id in ids.Where(id => !result.ContainsKey(id)))
        {
            result[id] = RatingSummary.Create(id, entityType);
        }

        return Result.Success(result);
    }

    public async Task<Result<Dictionary<Guid, Rating>>> GetUserRatingsBatchAsync(
        IEnumerable<Guid> entityIds,
        string entityType,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var ids = entityIds.ToList();

        var ratings = await _db.Set<Rating>()
            .Where(r => 
                ids.Contains(r.EntityId) && 
                r.EntityType == entityType && 
                r.UserId == userId && 
                !r.IsDeleted)
            .ToListAsync(ct);

        var result = ratings.ToDictionary(r => r.EntityId, r => r);
        return Result.Success(result);
    }

    // ─── Review Interactions ─────────────────────────────────────────────────────

    public async Task<Result> VoteHelpfulAsync(Guid ratingId, bool isHelpful, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();

        var rating = await _db.Set<Rating>()
            .FirstOrDefaultAsync(r => r.Id == ratingId && !r.IsDeleted, ct);

        if (rating == null)
            return Result.Failure(RatingErrors.NotFound);

        if (rating.UserId == userId)
            return Result.Failure(RatingErrors.CannotVoteOwnRating);

        var existingVote = await _db.Set<RatingHelpfulVote>()
            .FirstOrDefaultAsync(v => v.RatingId == ratingId && v.UserId == userId && !v.IsDeleted, ct);

        if (existingVote != null)
        {
            // Update existing vote
            var wasHelpful = existingVote.IsHelpful;
            existingVote.UpdateVote(isHelpful);

            if (wasHelpful && !isHelpful)
            {
                rating.DecrementHelpful();
            }
            else if (!wasHelpful && isHelpful)
            {
                rating.IncrementHelpful();
            }
        }
        else
        {
            // Create new vote
            var vote = RatingHelpfulVote.Create(ratingId, userId, isHelpful);
            _db.Set<RatingHelpfulVote>().Add(vote);

            if (isHelpful)
                rating.IncrementHelpful();
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveHelpfulVoteAsync(Guid ratingId, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();

        var vote = await _db.Set<RatingHelpfulVote>()
            .FirstOrDefaultAsync(v => v.RatingId == ratingId && v.UserId == userId && !v.IsDeleted, ct);

        if (vote == null)
            return Result.Failure(RatingErrors.VoteNotFound);

        var rating = await _db.Set<Rating>()
            .FirstOrDefaultAsync(r => r.Id == ratingId && !r.IsDeleted, ct);

        if (rating != null && vote.IsHelpful)
            rating.DecrementHelpful();

        vote.IsDeleted = true;
        vote.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> ReportAsync(Guid ratingId, string reason, CancellationToken ct = default)
    {
        var rating = await _db.Set<Rating>()
            .FirstOrDefaultAsync(r => r.Id == ratingId && !r.IsDeleted, ct);

        if (rating == null)
            return Result.Failure(RatingErrors.NotFound);

        rating.IncrementReport();

        // Auto-flag for moderation if too many reports
        if (rating.ReportCount >= 3)
            rating.SetModerationStatus(RatingModerationStatus.Flagged);

        await _db.SaveChangesAsync(ct);

        _logger.LogWarning("Rating {RatingId} reported for: {Reason}. Total reports: {ReportCount}", 
            ratingId, reason, rating.ReportCount);

        return Result.Success();
    }

    // ─── Moderation (Admin) ──────────────────────────────────────────────────────

    public async Task<Result<IEnumerable<Rating>>> GetPendingModerationAsync(int skip = 0, int take = 20, CancellationToken ct = default)
    {
        var ratings = await _db.Set<Rating>()
            .Where(r => !r.IsDeleted)
            .Where(r => r.ModerationStatus == RatingModerationStatus.Pending || 
                        r.ModerationStatus == RatingModerationStatus.Flagged)
            .OrderByDescending(r => r.ReportCount)
            .ThenBy(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return Result.Success<IEnumerable<Rating>>(ratings);
    }

    public async Task<Result> ApproveAsync(Guid ratingId, CancellationToken ct = default)
    {
        var rating = await _db.Set<Rating>()
            .FirstOrDefaultAsync(r => r.Id == ratingId && !r.IsDeleted, ct);

        if (rating == null)
            return Result.Failure(RatingErrors.NotFound);

        rating.SetModerationStatus(RatingModerationStatus.Approved);
        await _db.SaveChangesAsync(ct);

        // Recalculate summary since this rating is now visible
        await RecalculateSummaryAsync(rating.EntityId, rating.EntityType, ct);

        return Result.Success();
    }

    public async Task<Result> RejectAsync(Guid ratingId, CancellationToken ct = default)
    {
        var rating = await _db.Set<Rating>()
            .FirstOrDefaultAsync(r => r.Id == ratingId && !r.IsDeleted, ct);

        if (rating == null)
            return Result.Failure(RatingErrors.NotFound);

        rating.SetModerationStatus(RatingModerationStatus.Rejected);
        await _db.SaveChangesAsync(ct);

        // Recalculate summary since this rating is now hidden
        await RecalculateSummaryAsync(rating.EntityId, rating.EntityType, ct);

        return Result.Success();
    }

    public async Task<Result> AdminDeleteAsync(Guid ratingId, CancellationToken ct = default)
    {
        var rating = await _db.Set<Rating>()
            .FirstOrDefaultAsync(r => r.Id == ratingId && !r.IsDeleted, ct);

        if (rating == null)
            return Result.Failure(RatingErrors.NotFound);

        rating.IsDeleted = true;
        rating.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogWarning("Admin deleted rating {RatingId}", ratingId);

        // Recalculate summary
        await RecalculateSummaryAsync(rating.EntityId, rating.EntityType, ct);

        return Result.Success();
    }

    // ─── Statistics & Analytics ──────────────────────────────────────────────────

    public async Task<Result> RecalculateSummaryAsync(Guid entityId, string entityType, CancellationToken ct = default)
    {
        var summary = await _db.Set<RatingSummary>()
            .FirstOrDefaultAsync(s => s.EntityId == entityId && s.EntityType == entityType && !s.IsDeleted, ct);

        if (summary == null)
        {
            summary = RatingSummary.Create(entityId, entityType);
            _db.Set<RatingSummary>().Add(summary);
        }

        var ratings = await _db.Set<Rating>()
            .Where(r => r.EntityId == entityId && r.EntityType == entityType && !r.IsDeleted)
            .ToListAsync(ct);

        summary.Recalculate(ratings);
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result<IEnumerable<RatingSummary>>> GetTopRatedAsync(
        string entityType,
        int minRatings = 5,
        int take = 10,
        CancellationToken ct = default)
    {
        var summaries = await _db.Set<RatingSummary>()
            .Where(s => s.EntityType == entityType && !s.IsDeleted)
            .Where(s => s.TotalRatings >= minRatings)
            .OrderByDescending(s => s.AverageRating)
            .ThenByDescending(s => s.TotalRatings)
            .Take(take)
            .ToListAsync(ct);

        return Result.Success<IEnumerable<RatingSummary>>(summaries);
    }

    public async Task<Result<IEnumerable<Rating>>> GetRecentReviewsAsync(
        string? entityType = null,
        int take = 20,
        CancellationToken ct = default)
    {
        var query = _db.Set<Rating>()
            .Where(r => !r.IsDeleted)
            .Where(r => r.ModerationStatus == RatingModerationStatus.Approved)
            .Where(r => r.ReviewText != null && r.ReviewText != "");

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(r => r.EntityType == entityType);

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

        return Result.Success<IEnumerable<Rating>>(reviews);
    }
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
