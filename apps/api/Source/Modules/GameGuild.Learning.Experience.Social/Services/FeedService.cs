using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Experience.Social.Services;

/// <summary>
/// Service implementation for personalized feed operations
/// </summary>
public class FeedService : IFeedService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<FeedService> _logger;

    public FeedService(
        IApplicationDbContext context,
        ILogger<FeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<PersonalizedFeedItem>>> GetPersonalizedFeedAsync(
        Guid userId,
        int skip = 0,
        int take = 20,
        FeedItemType? filterByType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<PersonalizedFeedItem>()
            .Where(f => f.UserId == userId && !f.IsDismissed && f.ExpiresAt > DateTime.UtcNow);

        if (filterByType.HasValue)
        {
            query = query.Where(f => f.ItemType == filterByType.Value);
        }

        var items = await query
            .OrderByDescending(f => f.RelevanceScore)
            .ThenByDescending(f => f.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<IEnumerable<PersonalizedFeedItem>>(items);
    }

    public async Task<Result<int>> GenerateFeedItemsAsync(Guid userId, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        // This is a simplified feed generation - in production, this would use
        // more sophisticated algorithms based on user behavior, preferences, etc.
        var generatedCount = 0;

        // Get trending discussions (most replied in last 7 days)
        var trendingDiscussions = await _context.Set<CourseDiscussion>()
            .Where(d => d.LastActivityAt > DateTime.UtcNow.AddDays(-7))
            .OrderByDescending(d => d.ReplyCount)
            .Take(5)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var discussion in trendingDiscussions)
        {
            var feedItem = PersonalizedFeedItem.Create(
                userId,
                FeedItemType.TrendingDiscussion,
                tenantId,
                courseId: discussion.CourseId,
                discussionId: discussion.Id,
                relevanceScore: 0.7,
                reason: $"Trending discussion with {discussion.ReplyCount} replies");

            _context.Set<PersonalizedFeedItem>().Add(feedItem);
            generatedCount++;
        }

        // Get featured reviews
        var featuredReviews = await _context.Set<CourseReview>()
            .Where(r => r.IsFeatured && r.IsApproved)
            .OrderByDescending(r => r.HelpfulCount)
            .Take(3)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var review in featuredReviews)
        {
            var feedItem = PersonalizedFeedItem.Create(
                userId,
                FeedItemType.FeaturedReview,
                tenantId,
                courseId: review.CourseId,
                reviewId: review.Id,
                relevanceScore: 0.8,
                reason: "Featured review");

            _context.Set<PersonalizedFeedItem>().Add(feedItem);
            generatedCount++;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Generated {Count} feed items for user {UserId}", generatedCount, userId);
        return Result.Success(generatedCount);
    }

    public async Task<Result<PersonalizedFeedItem>> MarkFeedItemViewedAsync(Guid feedItemId, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<PersonalizedFeedItem>()
            .FirstOrDefaultAsync(f => f.Id == feedItemId, cancellationToken).ConfigureAwait(false);

        if (item == null)
        {
            return Result.Failure<PersonalizedFeedItem>(Error.NotFound("FeedItem.NotFound", $"Feed item with ID {feedItemId} not found"));
        }

        item.MarkViewed();
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(item);
    }

    public async Task<Result<PersonalizedFeedItem>> DismissFeedItemAsync(Guid feedItemId, Guid userId, CancellationToken cancellationToken = default)
    {
        var item = await _context.Set<PersonalizedFeedItem>()
            .FirstOrDefaultAsync(f => f.Id == feedItemId, cancellationToken).ConfigureAwait(false);

        if (item == null)
        {
            return Result.Failure<PersonalizedFeedItem>(Error.NotFound("FeedItem.NotFound", $"Feed item with ID {feedItemId} not found"));
        }

        if (item.UserId != userId)
        {
            return Result.Failure<PersonalizedFeedItem>(Error.Failure("FeedItem.Unauthorized", "You can only dismiss your own feed items"));
        }

        item.Dismiss();
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(item);
    }

    public async Task<Result<int>> ClearExpiredFeedItemsAsync(CancellationToken cancellationToken = default)
    {
        var expiredItems = await _context.Set<PersonalizedFeedItem>()
            .Where(f => f.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        _context.Set<PersonalizedFeedItem>().RemoveRange(expiredItems);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Cleared {Count} expired feed items", expiredItems.Count);
        return Result.Success(expiredItems.Count);
    }
}
