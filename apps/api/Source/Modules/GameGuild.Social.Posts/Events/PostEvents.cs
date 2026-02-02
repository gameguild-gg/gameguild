namespace GameGuild.Social.Posts.Events;

/// <summary>
/// Domain event raised when a new post is created
/// </summary>
public sealed record PostCreatedEvent(
    Guid PostId,
    Guid AuthorId,
    string Content,
    PostVisibility Visibility,
    Guid? TenantId,
    DateTime CreatedAt) : DomainEventBase(PostId, nameof(Post));

/// <summary>
/// Domain event raised when a post is updated
/// </summary>
public sealed record PostUpdatedEvent(
    Guid PostId,
    Guid AuthorId,
    string OldContent,
    string NewContent,
    DateTime UpdatedAt,
    Guid? TenantId) : DomainEventBase(PostId, nameof(Post));

/// <summary>
/// Domain event raised when a post is deleted
/// </summary>
public sealed record PostDeletedEvent(
    Guid PostId,
    Guid AuthorId,
    bool IsSoftDelete,
    DateTime DeletedAt,
    Guid? TenantId) : DomainEventBase(PostId, nameof(Post));

/// <summary>
/// Domain event raised when a post is liked
/// </summary>
public sealed record PostLikedEvent(
    Guid PostId,
    Guid AuthorId,
    Guid LikedByUserId,
    string ReactionType,
    int NewLikesCount,
    DateTime LikedAt,
    Guid? TenantId) : DomainEventBase(PostId, nameof(Post));

/// <summary>
/// Domain event raised when a post is unliked
/// </summary>
public sealed record PostUnlikedEvent(
    Guid PostId,
    Guid AuthorId,
    Guid UnlikedByUserId,
    int NewLikesCount,
    DateTime UnlikedAt,
    Guid? TenantId) : DomainEventBase(PostId, nameof(Post));

/// <summary>
/// Domain event raised when a comment is added to a post
/// </summary>
public sealed record PostCommentedEvent(
    Guid PostId,
    Guid CommentId,
    Guid AuthorId,
    Guid CommenterId,
    Guid? ParentCommentId,
    int NewCommentsCount,
    DateTime CommentedAt,
    Guid? TenantId) : DomainEventBase(PostId, nameof(Post));

/// <summary>
/// Domain event raised when a post is shared
/// </summary>
public sealed record PostSharedEvent(
    Guid PostId,
    Guid AuthorId,
    Guid? SharedByUserId,
    int NewSharesCount,
    DateTime SharedAt,
    Guid? TenantId) : DomainEventBase(PostId, nameof(Post));

/// <summary>
/// Domain event raised when a post is pinned
/// </summary>
public sealed record PostPinnedEvent(
    Guid PostId,
    Guid AuthorId,
    bool IsPinned,
    DateTime PinnedAt,
    Guid? TenantId) : DomainEventBase(PostId, nameof(Post));

/// <summary>
/// Domain event raised when a post is viewed
/// </summary>
public sealed record PostViewedEvent(
    Guid PostId,
    Guid ViewId,
    Guid? ViewerId,
    bool IsUniqueViewer,
    int NewViewsCount,
    DateTime ViewedAt,
    Guid? TenantId) : DomainEventBase(PostId, nameof(Post));

/// <summary>
/// Domain event raised when post becomes trending
/// </summary>
public sealed record PostTrendingEvent(
    Guid PostId,
    Guid AuthorId,
    double TrendingScore,
    int TrendingRank,
    DateTime TrendingAt,
    Guid? TenantId) : DomainEventBase(PostId, nameof(Post));

/// <summary>
/// Domain event raised when a tag is added to a post
/// </summary>
public sealed record PostTaggedEvent(
    Guid PostId,
    Guid TagId,
    string TagName,
    DateTime TaggedAt,
    Guid? TenantId) : DomainEventBase(PostId, nameof(Post));

/// <summary>
/// Domain event raised when a content reference is added to a post
/// </summary>
public sealed record PostContentReferencedEvent(
    Guid PostId,
    Guid ReferenceId,
    Guid ReferencedResourceId,
    string ResourceType,
    string ReferenceType,
    DateTime ReferencedAt,
    Guid? TenantId) : DomainEventBase(PostId, nameof(Post));

/// <summary>
/// Base class for post domain events
/// </summary>
public abstract record DomainEventBase(Guid EntityId, string EntityType)
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
