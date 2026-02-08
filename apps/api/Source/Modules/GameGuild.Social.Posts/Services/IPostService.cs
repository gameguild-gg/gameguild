
namespace GameGuild.Social.Posts.Services;

/// <summary>
/// Composite service interface for backward compatibility — inherits all post sub-service interfaces
/// </summary>
public interface IPostService : IPostCrudService, IPostEngagementService, IPostCommentService, IPostTagService, IPostContentReferenceService
{
}

/// <summary>
/// Service interface for system-generated announcements and automated posts
/// </summary>
public interface IPostAnnouncementService
{
    /// <summary>Creates a system announcement post</summary>
    Task<Result<Post>> CreateSystemAnnouncementAsync(
        Guid tenantId,
        Guid authorId,
        string title,
        string message,
        string priority = "normal",
        CancellationToken cancellationToken = default);

    /// <summary>Creates a milestone celebration post</summary>
    Task<Result<Post>> CreateMilestoneCelebrationAsync(
        Guid tenantId,
        Guid authorId,
        string milestoneName,
        string description,
        DateTime achievementDate,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a community update post</summary>
    Task<Result<Post>> CreateCommunityUpdateAsync(
        Guid tenantId,
        Guid authorId,
        string title,
        string content,
        string targetAudience = "all",
        CancellationToken cancellationToken = default);

    /// <summary>Creates a welcome post for a new user</summary>
    Task<Result<Post>> CreateWelcomePostAsync(
        Guid tenantId,
        Guid userId,
        string userName,
        CancellationToken cancellationToken = default);
}
