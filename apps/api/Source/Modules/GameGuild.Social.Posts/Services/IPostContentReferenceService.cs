namespace GameGuild.Social.Posts.Services;

/// <summary>
/// Service interface for post content reference management
/// </summary>
public interface IPostContentReferenceService
{
    /// <summary>Adds a content reference to a post</summary>
    Task<Result<PostContentReference>> AddContentReferenceAsync(
        Guid postId,
        Guid resourceId,
        string resourceType,
        string referenceType = "mention",
        string? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a content reference from a post</summary>
    Task<Result> RemoveContentReferenceAsync(Guid referenceId, CancellationToken cancellationToken = default);

    /// <summary>Gets content references for a post</summary>
    Task<Result<IEnumerable<PostContentReference>>> GetPostContentReferencesAsync(Guid postId, CancellationToken cancellationToken = default);
}
