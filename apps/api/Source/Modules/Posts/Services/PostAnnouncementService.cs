using GameGuild.Common;
using GameGuild.Database;


namespace GameGuild.Modules.Posts.Services;

/// <summary>
/// Service for creating system-generated announcement posts
/// </summary>
public interface IPostAnnouncementService {
  /// <summary>
  /// Creates a system announcement post
  /// </summary>
  Task<GameGuild.Common.Result<Post>> CreateSystemAnnouncementAsync(
    Guid tenantId,
    Guid authorId,
    string title,
    string message,
    string priority,
    CancellationToken cancellationToken = default
  );

  /// <summary>
  /// Creates a milestone celebration post
  /// </summary>
  Task<GameGuild.Common.Result<Post>> CreateMilestoneCelebrationAsync(
    Guid tenantId,
    Guid authorId,
    string milestoneName,
    string description,
    DateTime achievementDate,
    CancellationToken cancellationToken = default
  );

  /// <summary>
  /// Creates a community update post
  /// </summary>
  Task<GameGuild.Common.Result<Post>> CreateCommunityUpdateAsync(
    Guid tenantId,
    Guid authorId,
    string title,
    string content,
    string targetAudience,
    CancellationToken cancellationToken = default
  );
}

/// <summary>
/// Implementation of post announcement service
/// </summary>
public class PostAnnouncementService : IPostAnnouncementService {
  private readonly ApplicationDbContext _context;
  private readonly IDomainEventPublisher _eventPublisher;
  private readonly ILogger<PostAnnouncementService> _logger;

  public PostAnnouncementService(
    ApplicationDbContext context,
    IDomainEventPublisher eventPublisher,
    ILogger<PostAnnouncementService> logger
  ) {
    _context = context;
    _eventPublisher = eventPublisher;
    _logger = logger;
  }

  public async Task<GameGuild.Common.Result<Post>> CreateSystemAnnouncementAsync(
    Guid tenantId,
    Guid authorId,
    string title,
    string message,
    string priority,
    CancellationToken cancellationToken = default
  ) {
    try {
      _logger.LogInformation("Creating system announcement: {Title}", title);

      var announcementPost = new Post {
        Id = Guid.NewGuid(),
        Title = title,
        Description = message,
        Slug = GenerateSlug($"announcement-{title}-{DateTime.UtcNow:yyyyMMdd}"),
        PostType = "system_announcement",
        AuthorId = authorId,
        IsSystemGenerated = true,
        Visibility = GameGuild.Modules.Contents.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        LikesCount = 0,
        CommentsCount = 0,
        SharesCount = 0,
        IsPinned = true // System announcements are typically pinned
      };

      // Set tenant
      var tenant = await _context.Tenants.FindAsync(tenantId, cancellationToken);

      if (tenant != null) { announcementPost.Tenant = tenant; }

      _context.Posts.Add(announcementPost);
      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Successfully created system announcement {PostId}", announcementPost.Id);

      // Publish domain event
      await _eventPublisher.PublishAsync(
        new PostCreatedEvent(
          announcementPost.Id,
          announcementPost.AuthorId,
          announcementPost.Description ?? "",
          announcementPost.PostType,
          announcementPost.IsSystemGenerated,
          announcementPost.CreatedAt,
          announcementPost.Tenant?.Id ?? Guid.Empty
        ),
        cancellationToken
      );

      return GameGuild.Common.Result.Success(announcementPost);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Failed to create system announcement: {Title}", title);

      return GameGuild.Common.Result.Failure<Post>(
        new GameGuild.Common.Error(
          "PostAnnouncement.CreationFailed",
          $"Failed to create system announcement: {ex.Message}",
          ErrorType.Failure
        )
      );
    }
  }

  public async Task<GameGuild.Common.Result<Post>> CreateMilestoneCelebrationAsync(
    Guid tenantId,
    Guid authorId,
    string milestoneName,
    string description,
    DateTime achievementDate,
    CancellationToken cancellationToken = default
  ) {
    try {
      _logger.LogInformation("Creating milestone celebration for user {UserId}: {Milestone}", authorId, milestoneName);

      var celebrationPost = new Post {
        Id = Guid.NewGuid(),
        Title = $"🎉 Milestone Achieved: {milestoneName}",
        Description = $"🎉 This is a celebration! {description}",
        Slug = GenerateSlug($"milestone-{milestoneName}-{authorId}-{DateTime.UtcNow:yyyyMMdd}"),
        PostType = "milestone_celebration",
        AuthorId = authorId,
        IsSystemGenerated = true,
        Visibility = GameGuild.Modules.Contents.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        LikesCount = 0,
        CommentsCount = 0,
        SharesCount = 0,
        IsPinned = false
      };

      // Set tenant
      var tenant = await _context.Tenants.FindAsync(tenantId, cancellationToken);

      if (tenant != null) { celebrationPost.Tenant = tenant; }

      _context.Posts.Add(celebrationPost);
      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogInformation(
        "Successfully created milestone celebration {PostId} for user {UserId}",
        celebrationPost.Id,
        authorId
      );

      // Publish domain event
      await _eventPublisher.PublishAsync(
        new PostCreatedEvent(
          celebrationPost.Id,
          celebrationPost.AuthorId,
          celebrationPost.Description ?? "",
          celebrationPost.PostType,
          celebrationPost.IsSystemGenerated,
          celebrationPost.CreatedAt,
          celebrationPost.Tenant?.Id ?? Guid.Empty
        ),
        cancellationToken
      );

      return GameGuild.Common.Result.Success(celebrationPost);
    }
    catch (Exception ex) {
      _logger.LogError(
        ex,
        "Failed to create milestone celebration for user {UserId}: {Milestone}",
        authorId,
        milestoneName
      );

      return GameGuild.Common.Result.Failure<Post>(
        new GameGuild.Common.Error(
          "PostAnnouncement.MilestoneCreationFailed",
          $"Failed to create milestone celebration: {ex.Message}",
          ErrorType.Failure
        )
      );
    }
  }

  public async Task<GameGuild.Common.Result<Post>> CreateCommunityUpdateAsync(
    Guid tenantId,
    Guid authorId,
    string title,
    string content,
    string targetAudience,
    CancellationToken cancellationToken = default
  ) {
    try {
      _logger.LogInformation("Creating community update: {UpdateTitle}", title);

      var updatePost = new Post {
        Id = Guid.NewGuid(),
        Title = $"📢 Community Update: {title}",
        Description = content,
        Slug = GenerateSlug($"community-update-{title}-{DateTime.UtcNow:yyyyMMdd}"),
        PostType = "community_update",
        AuthorId = authorId,
        IsSystemGenerated = true,
        Visibility = GameGuild.Modules.Contents.AccessLevel.Public,
        Status = GameGuild.Modules.Contents.ContentStatus.Published,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        LikesCount = 0,
        CommentsCount = 0,
        SharesCount = 0,
        IsPinned = true
      };

      // Set tenant
      var tenant = await _context.Tenants.FindAsync(tenantId, cancellationToken);

      if (tenant != null) { updatePost.Tenant = tenant; }

      _context.Posts.Add(updatePost);
      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Successfully created community update {PostId}", updatePost.Id);

      // Publish domain event
      await _eventPublisher.PublishAsync(
        new PostCreatedEvent(
          updatePost.Id,
          updatePost.AuthorId,
          updatePost.Description ?? "",
          updatePost.PostType,
          updatePost.IsSystemGenerated,
          updatePost.CreatedAt,
          updatePost.Tenant?.Id ?? Guid.Empty
        ),
        cancellationToken
      );

      return GameGuild.Common.Result.Success(updatePost);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Failed to create community update: {UpdateTitle}", title);

      return GameGuild.Common.Result.Failure<Post>(
        new GameGuild.Common.Error(
          "PostAnnouncement.CommunityUpdateFailed",
          $"Failed to create community update: {ex.Message}",
          ErrorType.Failure
        )
      );
    }
  }

  private static string GenerateSlug(string title) {
    return title
           .ToLowerInvariant()
           .Replace(" ", "-")
           .Replace(".", "")
           .Replace(",", "")
           .Replace("'", "")
           .Replace("\"", "")
           .Replace("!", "")
           .Replace("?", "")
           .Replace("&", "and")
           .Replace("@", "at")
           .Replace(":", "")
           .Replace(";", "")
           .Replace("(", "")
           .Replace(")", "")
           .Replace("[", "")
           .Replace("]", "")
           .Replace("{", "")
           .Replace("}", "")
           .Replace("#", "")
           .Replace("%", "")
           .Replace("*", "")
           .Replace("+", "")
           .Replace("=", "")
           .Replace("/", "-")
           .Replace("\\", "-");
  }
}
