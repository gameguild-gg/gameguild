using GameGuild.Common;


namespace GameGuild.Modules.Posts;

/// <summary>
/// Event raised when a post is created
/// </summary>
public sealed class PostCreatedEvent(
  Guid postId,
  Guid userId,
  string content,
  string postType,
  bool isSystemGenerated,
  DateTime createdAt,
  Guid tenantId
) : DomainEventBase(postId, nameof(Post)) {
  public Guid PostId { get; } = postId;

  public Guid UserId { get; } = userId;

  public string Content { get; } = content;

  public string PostType { get; } = postType;

  public bool IsSystemGenerated { get; } = isSystemGenerated;

  public DateTime CreatedAt { get; } = createdAt;

  public Guid TenantId { get; } = tenantId;
}

/// <summary>
/// Event raised when a post is updated
/// </summary>
public sealed class PostUpdatedEvent(
  Guid postId,
  Guid userId,
  Dictionary<string, object> changes,
  DateTime updatedAt,
  Guid tenantId
) : DomainEventBase(postId, nameof(Post)) {
  public Guid PostId { get; } = postId;

  public Guid UserId { get; } = userId;

  public Dictionary<string, object> Changes { get; } = changes;

  public DateTime UpdatedAt { get; } = updatedAt;

  public Guid TenantId { get; } = tenantId;
}

/// <summary>
/// Event raised when a post is deleted
/// </summary>
public sealed class PostDeletedEvent(
  Guid postId,
  Guid userId,
  string postType,
  bool isSoftDelete,
  DateTime deletedAt,
  Guid tenantId
) : DomainEventBase(postId, nameof(Post)) {
  public Guid PostId { get; } = postId;

  public Guid UserId { get; } = userId;

  public string PostType { get; } = postType;

  public bool IsSoftDelete { get; } = isSoftDelete;

  public DateTime DeletedAt { get; } = deletedAt;

  public Guid TenantId { get; } = tenantId;
}

/// <summary>
/// Event raised when a post is liked
/// </summary>
public sealed class PostLikedEvent(
  Guid postId,
  Guid userId,
  Guid likedByUserId,
  int newLikesCount,
  DateTime likedAt,
  Guid tenantId
) : DomainEventBase(postId, nameof(Post)) {
  public Guid PostId { get; } = postId;

  public Guid UserId { get; } = userId; // Post owner

  public Guid LikedByUserId { get; } = likedByUserId; // User who liked

  public int NewLikesCount { get; } = newLikesCount;

  public DateTime LikedAt { get; } = likedAt;

  public Guid TenantId { get; } = tenantId;
}
