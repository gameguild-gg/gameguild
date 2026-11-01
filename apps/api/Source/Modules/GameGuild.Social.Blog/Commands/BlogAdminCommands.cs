using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Social.Blog.Services;

namespace GameGuild.Social.Blog.Commands;

/// <summary>SystemAdmin takedown: force-unpublish a post regardless of authorship (issue #96 admin surface).</summary>
public sealed record AdminUnpublishBlogPostCommand(Guid PostId) : ICommand<BlogPost>;

/// <summary>SystemAdmin removal: soft-delete a post regardless of authorship (issue #96 admin surface).</summary>
public sealed record AdminDeleteBlogPostCommand(Guid PostId) : ICommand<Unit>;

/// <summary>SystemAdmin moderation: soft-delete any comment; the moderator is recorded on the row (issue #96 admin surface).</summary>
public sealed record AdminDeleteBlogCommentCommand(Guid CommentId) : ICommand<Unit>;

/// <summary>
/// Imports a Ghost export into the internal blog (issue #96 content migration). Idempotent on
/// (author, slug); dry-run plans without persisting. Fallback author defaults to the moderator.
/// </summary>
public sealed record ImportGhostContentCommand(string ExportJson, bool DryRun, bool PublishImported) : ICommand<GhostImportReport>;

/// <summary>
/// Admin command handlers (issue #96 admin surface). Defense in depth beyond the controller's
/// SystemAdmin policy: each handler re-asserts the actor carries the SystemAdmin role and fails
/// closed otherwise (invariant #6). The acting moderator identity always comes from the actor
/// context (invariant #8).
/// </summary>
public static class BlogAdminCommandHandlers
{
    /// <summary>Extracts the authenticated actor user id and asserts the SystemAdmin role.</summary>
    public static Guid RequireSystemAdmin(IActorContextAccessor actorContext)
    {
        var actor = actorContext.ActorContext;
        if (!actor.IsAuthenticated || !actor.IsSystemAdmin || actor.SubjectIdAsGuid is not { } userId || userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("SystemAdmin is required for blog administration.");
        }

        return userId;
    }
}

/// <summary>Handles <see cref="AdminUnpublishBlogPostCommand"/>.</summary>
public sealed class AdminUnpublishBlogPostCommandHandler(IBlogPostService service, IActorContextAccessor actorContext)
    : ICommandHandler<AdminUnpublishBlogPostCommand, BlogPost>
{
    /// <inheritdoc />
    public Task<BlogPost> Handle(AdminUnpublishBlogPostCommand request, CancellationToken cancellationToken)
        => service.AdminUnpublishAsync(BlogAdminCommandHandlers.RequireSystemAdmin(actorContext), request.PostId, cancellationToken);
}

/// <summary>Handles <see cref="AdminDeleteBlogPostCommand"/>.</summary>
public sealed class AdminDeleteBlogPostCommandHandler(IBlogPostService service, IActorContextAccessor actorContext)
    : ICommandHandler<AdminDeleteBlogPostCommand, Unit>
{
    /// <inheritdoc />
    public async Task<Unit> Handle(AdminDeleteBlogPostCommand request, CancellationToken cancellationToken)
    {
        await service.AdminDeletePostAsync(BlogAdminCommandHandlers.RequireSystemAdmin(actorContext), request.PostId, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

/// <summary>Handles <see cref="AdminDeleteBlogCommentCommand"/>.</summary>
public sealed class AdminDeleteBlogCommentCommandHandler(IBlogPostService service, IActorContextAccessor actorContext)
    : ICommandHandler<AdminDeleteBlogCommentCommand, Unit>
{
    /// <inheritdoc />
    public async Task<Unit> Handle(AdminDeleteBlogCommentCommand request, CancellationToken cancellationToken)
    {
        await service.AdminDeleteCommentAsync(BlogAdminCommandHandlers.RequireSystemAdmin(actorContext), request.CommentId, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

/// <summary>Handles <see cref="ImportGhostContentCommand"/>.</summary>
public sealed class ImportGhostContentCommandHandler(IGhostImportService importService, IActorContextAccessor actorContext)
    : ICommandHandler<ImportGhostContentCommand, GhostImportReport>
{
    /// <inheritdoc />
    public Task<GhostImportReport> Handle(ImportGhostContentCommand request, CancellationToken cancellationToken)
    {
        // The moderator is the default fallback author for Ghost authors without a platform profile.
        var moderator = BlogAdminCommandHandlers.RequireSystemAdmin(actorContext);
        return importService.ImportAsync(
            request.ExportJson,
            new GhostImportOptions(request.DryRun, request.PublishImported, FallbackAuthorId: moderator),
            cancellationToken);
    }
}
