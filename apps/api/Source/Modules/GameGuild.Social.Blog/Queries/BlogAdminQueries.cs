using GameGuild.CQRS;
using GameGuild.Social.Blog.Services;
using GameGuild.Social.Profiles;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Social.Blog.Queries;

/// <summary>
/// Admin card row for one post across ALL authors (drafts included). List surfaces
/// never ship the body. Gated by the SystemAdmin policy on <see cref="Controllers.BlogAdminController"/>.
/// </summary>
public sealed record BlogAdminPostDto(
    Guid Id,
    string PrimaryAuthorHandle,
    string Title,
    string Slug,
    BlogPostStatus Status,
    DateTime? PublishedAt,
    DateTime UpdatedAt,
    int CommentsCount,
    int ViewsCount,
    bool AllowComments,
    IReadOnlyList<string> Tags);

/// <summary>Page of admin post rows, newest edit first.</summary>
public sealed record BlogAdminPostPage(IReadOnlyList<BlogAdminPostDto> Items, bool HasMore);

/// <summary>
/// Admin moderation row for one comment: the comment plus enough post/author context
/// to review and act on it without extra round trips.
/// </summary>
public sealed record BlogAdminCommentDto(
    Guid Id,
    Guid BlogPostId,
    string PostTitle,
    string PostHandle,
    string PostSlug,
    Guid? ParentCommentId,
    string AuthorHandle,
    string Content,
    DateTime CreatedAt);

/// <summary>Page of admin comment rows, newest first.</summary>
public sealed record BlogAdminCommentPage(IReadOnlyList<BlogAdminCommentDto> Items, bool HasMore);

/// <summary>Platform-wide blog counters for the admin console header.</summary>
public sealed record BlogAdminStatsDto(int PublishedPosts, int DraftPosts, int CommentsLast30Days);

/// <summary>Lists every live post (any status, any author), newest edit first (issue #96 admin surface).</summary>
public sealed record ListBlogAdminPostsQuery(int Page = 0, BlogPostStatus? Status = null) : IQuery<BlogAdminPostPage>;

/// <summary>Lists the newest comments platform-wide, newest first, for moderation (issue #96 admin surface).</summary>
public sealed record ListBlogAdminCommentsQuery(int Page = 0) : IQuery<BlogAdminCommentPage>;

/// <summary>Platform-wide blog counters (issue #96 admin surface).</summary>
public sealed record GetBlogAdminStatsQuery() : IQuery<BlogAdminStatsDto>;

/// <summary>Admin read-side handlers. The SystemAdmin gate lives on the controller; these queries still exclude soft-deleted rows.</summary>
public static class BlogAdminQueries
{
    internal const int PageSize = 20;
}

/// <summary>Handles <see cref="ListBlogAdminPostsQuery"/>.</summary>
public sealed class ListBlogAdminPostsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<ListBlogAdminPostsQuery, BlogAdminPostPage>
{
    /// <inheritdoc />
    public async Task<BlogAdminPostPage> Handle(ListBlogAdminPostsQuery request, CancellationToken cancellationToken)
    {
        var page = await context.Set<BlogPost>()
            .AsNoTracking()
            .Where(p => p.DeletedAt == null && (request.Status == null || p.Status == request.Status))
            .OrderByDescending(p => p.UpdatedAt)
            .ThenByDescending(p => p.Id)
            .Skip(Math.Max(0, request.Page) * BlogAdminQueries.PageSize)
            .Take(BlogAdminQueries.PageSize + 1)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var profiles = await BlogPublicQueries.LoadProfilesAsync(
            context, [.. page.Select(p => p.PrimaryAuthorId)], cancellationToken).ConfigureAwait(false);

        var items = page
            .Take(BlogAdminQueries.PageSize)
            .Select(p =>
            {
                var profile = profiles.GetValueOrDefault(p.PrimaryAuthorId);
                return new BlogAdminPostDto(
                    p.Id,
                    profile.Handle ?? string.Empty,
                    p.Title,
                    p.Slug,
                    p.Status,
                    p.PublishedAt,
                    p.UpdatedAt,
                    p.CommentsCount,
                    p.ViewsCount,
                    p.AllowComments,
                    p.Tags);
            })
            .ToList();

        return new BlogAdminPostPage(items, page.Count > BlogAdminQueries.PageSize);
    }
}

/// <summary>Handles <see cref="ListBlogAdminCommentsQuery"/>.</summary>
public sealed class ListBlogAdminCommentsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<ListBlogAdminCommentsQuery, BlogAdminCommentPage>
{
    /// <inheritdoc />
    public async Task<BlogAdminCommentPage> Handle(ListBlogAdminCommentsQuery request, CancellationToken cancellationToken)
    {
        var page = await context.Set<BlogComment>()
            .AsNoTracking()
            .Where(c => c.DeletedAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip(Math.Max(0, request.Page) * BlogAdminQueries.PageSize)
            .Take(BlogAdminQueries.PageSize + 1)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var posts = await context.Set<BlogPost>()
            .AsNoTracking()
            .Where(p => page.Select(c => c.BlogPostId).Distinct().Contains(p.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var profiles = await BlogPublicQueries.LoadProfilesAsync(
            context,
            [.. page.Select(c => c.AuthorUserId).Concat(posts.Select(p => p.PrimaryAuthorId)).Distinct()],
            cancellationToken).ConfigureAwait(false);

        var items = page
            .Take(BlogAdminQueries.PageSize)
            .Select(c =>
            {
                var post = posts.FirstOrDefault(p => p.Id == c.BlogPostId);
                return new BlogAdminCommentDto(
                    c.Id,
                    c.BlogPostId,
                    post?.Title ?? string.Empty,
                    profiles.GetValueOrDefault(post?.PrimaryAuthorId ?? Guid.Empty).Handle ?? string.Empty,
                    post?.Slug ?? string.Empty,
                    c.ParentCommentId,
                    profiles.GetValueOrDefault(c.AuthorUserId).Handle ?? string.Empty,
                    c.Content,
                    c.CreatedAt);
            })
            .ToList();

        return new BlogAdminCommentPage(items, page.Count > BlogAdminQueries.PageSize);
    }
}

/// <summary>Handles <see cref="GetBlogAdminStatsQuery"/>.</summary>
public sealed class GetBlogAdminStatsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetBlogAdminStatsQuery, BlogAdminStatsDto>
{
    /// <inheritdoc />
    public async Task<BlogAdminStatsDto> Handle(GetBlogAdminStatsQuery request, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);
        var published = await context.Set<BlogPost>()
            .AsNoTracking()
            .CountAsync(p => p.DeletedAt == null && p.Status == BlogPostStatus.Published, cancellationToken)
            .ConfigureAwait(false);
        var drafts = await context.Set<BlogPost>()
            .AsNoTracking()
            .CountAsync(p => p.DeletedAt == null && p.Status == BlogPostStatus.Draft, cancellationToken)
            .ConfigureAwait(false);
        var comments = await context.Set<BlogComment>()
            .AsNoTracking()
            .CountAsync(c => c.DeletedAt == null && c.CreatedAt >= cutoff, cancellationToken)
            .ConfigureAwait(false);

        return new BlogAdminStatsDto(published, drafts, comments);
    }
}
