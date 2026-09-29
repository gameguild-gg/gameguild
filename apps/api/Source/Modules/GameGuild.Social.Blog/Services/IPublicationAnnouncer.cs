namespace GameGuild.Social.Blog.Services;

/// <summary>
/// Fan-out hook dispatched when a blog post is published (community Post + follower
/// notifications). Todo 5 replaces the no-op default with real dispatch; the interface
/// lives here to avoid module-order coupling.
/// </summary>
public interface IPublicationAnnouncer
{
    /// <summary>Announces a newly published blog post. Must never fail the publish itself.</summary>
    Task AnnounceBlogPublishedAsync(BlogPost post, CancellationToken ct = default);
}

/// <summary>No-op default until the announcer wiring lands (todo 5).</summary>
public sealed class NoOpPublicationAnnouncer : IPublicationAnnouncer
{
    /// <inheritdoc />
    public Task AnnounceBlogPublishedAsync(BlogPost post, CancellationToken ct = default)
        => Task.CompletedTask;
}
