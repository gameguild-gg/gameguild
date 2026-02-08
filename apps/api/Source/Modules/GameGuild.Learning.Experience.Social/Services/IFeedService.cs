namespace GameGuild.Learning.Experience.Social.Services;

/// <summary>
/// Service interface for personalized feed operations
/// </summary>
public interface IFeedService
{
    /// <summary>
    /// Gets personalized feed for a user
    /// </summary>
    Task<Result<IEnumerable<PersonalizedFeedItem>>> GetPersonalizedFeedAsync(
        Guid userId,
        int skip = 0,
        int take = 20,
        FeedItemType? filterByType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates new feed items for a user
    /// </summary>
    Task<Result<int>> GenerateFeedItemsAsync(Guid userId, Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a feed item as viewed
    /// </summary>
    Task<Result<PersonalizedFeedItem>> MarkFeedItemViewedAsync(Guid feedItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dismisses a feed item
    /// </summary>
    Task<Result<PersonalizedFeedItem>> DismissFeedItemAsync(Guid feedItemId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears expired feed items
    /// </summary>
    Task<Result<int>> ClearExpiredFeedItemsAsync(CancellationToken cancellationToken = default);
}
