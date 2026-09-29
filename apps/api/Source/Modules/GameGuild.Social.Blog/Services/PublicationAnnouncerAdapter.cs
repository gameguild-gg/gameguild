using GameGuild.Announcements.Contracts;
using GameGuild.CQRS;
using GameGuild.Social.Profiles;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Social.Blog.Services;

/// <summary>
/// Dispatches the real publication announcement via the mediator when a blog post is
/// published. Must never fail the publish itself.
/// </summary>
public sealed class PublicationAnnouncerAdapter(ISender sender, IApplicationDbContext context) : IPublicationAnnouncer
{
    public async Task AnnounceBlogPublishedAsync(BlogPost post, CancellationToken ct = default)
    {
        try
        {
            var coAuthorIds = await context.Set<BlogPostAuthor>()
                .AsNoTracking()
                .Where(author => author.BlogPostId == post.Id)
                .Select(author => author.UserId)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var handle = await context.Set<SocialProfile>()
                .AsNoTracking()
                .Where(profile => profile.UserId == post.PrimaryAuthorId && profile.DeletedAt == null)
                .Select(profile => profile.Handle)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            var canonicalUrl = post.CanonicalUrlOverride is { Length: > 0 } canonical
                ? canonical
                : handle is { Length: > 0 }
                    ? $"/blogs/{handle}/{post.Slug}"
                    : null;

            await sender.Send(new AnnouncePublicationCommand
            {
                Kind = PublicationKind.BlogPostPublished,
                ActorId = post.PrimaryAuthorId,
                Title = post.Title,
                EntityId = post.Id,
                Slug = post.Slug,
                CoAuthorIds = coAuthorIds,
                Excerpt = post.Excerpt,
                CanonicalUrl = canonicalUrl,
                TenantId = post.TenantId,
            }, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Announcements must never fail the publish that dispatched them.
            throw new InvalidOperationException(
                $"Blog publication announcement for post {post.Id} failed; see inner exception.", ex);
        }
    }
}
