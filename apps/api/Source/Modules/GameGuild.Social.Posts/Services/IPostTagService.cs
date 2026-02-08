namespace GameGuild.Social.Posts.Services;

/// <summary>
/// Service interface for post tag management
/// </summary>
public interface IPostTagService
{
    /// <summary>Adds tags to a post</summary>
    Task<Result> AddTagsToPostAsync(Guid postId, string[] tagNames, CancellationToken cancellationToken = default);

    /// <summary>Removes tags from a post</summary>
    Task<Result> RemoveTagsFromPostAsync(Guid postId, string[] tagNames, CancellationToken cancellationToken = default);

    /// <summary>Gets all tags for a post</summary>
    Task<Result<IEnumerable<PostTag>>> GetPostTagsAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>Gets or creates a tag by name</summary>
    Task<Result<PostTag>> GetOrCreateTagAsync(string name, string category = "general", CancellationToken cancellationToken = default);

    /// <summary>Gets popular tags</summary>
    Task<Result<IEnumerable<PostTag>>> GetPopularTagsAsync(int count = 20, CancellationToken cancellationToken = default);
}
