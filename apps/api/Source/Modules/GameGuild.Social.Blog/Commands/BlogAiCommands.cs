using GameGuild.CQRS;
using GameGuild.Social.Blog.Authoring;

namespace GameGuild.Social.Blog.Commands;

/// <summary>Starts (or idempotently replays) a blog AI authoring run.</summary>
public sealed record CreateBlogAiRunCommand(
    Guid TenantId,
    Guid ActorId,
    Guid PostId,
    BlogAiRunRequest Request) : ICommand<BlogAiRunDto>;

/// <summary>Cancels a blog AI run and releases its reservation.</summary>
public sealed record CancelBlogAiRunCommand(
    Guid TenantId,
    Guid ActorId,
    Guid PostId,
    Guid RunId) : ICommand<BlogAiRunDto>;

/// <summary>Applies a pending blog AI proposal to the post (revision-guarded).</summary>
public sealed record ApplyBlogAiProposalCommand(
    Guid TenantId,
    Guid ActorId,
    Guid PostId,
    Guid ProposalId,
    ApplyBlogAiProposalRequest Request) : ICommand<BlogPostDto>;

/// <summary>Discards a pending blog AI proposal.</summary>
public sealed record DiscardBlogAiProposalCommand(
    Guid TenantId,
    Guid ActorId,
    Guid PostId,
    Guid ProposalId) : ICommand<BlogAiProposalDto>;

/// <summary>Command handlers delegating to <see cref="IBlogAuthoringAiService"/>.</summary>
public sealed class BlogAiCommandHandler(IBlogAuthoringAiService authoring) :
    ICommandHandler<CreateBlogAiRunCommand, BlogAiRunDto>,
    ICommandHandler<CancelBlogAiRunCommand, BlogAiRunDto>,
    ICommandHandler<ApplyBlogAiProposalCommand, BlogPostDto>,
    ICommandHandler<DiscardBlogAiProposalCommand, BlogAiProposalDto>
{
    /// <inheritdoc />
    public Task<BlogAiRunDto> Handle(CreateBlogAiRunCommand request, CancellationToken cancellationToken) =>
        authoring.CreateRun(request.TenantId, request.ActorId, request.PostId, request.Request, cancellationToken);

    /// <inheritdoc />
    public Task<BlogAiRunDto> Handle(CancelBlogAiRunCommand request, CancellationToken cancellationToken) =>
        authoring.CancelRun(request.TenantId, request.ActorId, request.PostId, request.RunId, cancellationToken);

    /// <inheritdoc />
    public Task<BlogPostDto> Handle(ApplyBlogAiProposalCommand request, CancellationToken cancellationToken) =>
        authoring.ApplyProposal(request.TenantId, request.ActorId, request.PostId, request.ProposalId, request.Request, cancellationToken);

    /// <inheritdoc />
    public Task<BlogAiProposalDto> Handle(DiscardBlogAiProposalCommand request, CancellationToken cancellationToken) =>
        authoring.DiscardProposal(request.TenantId, request.ActorId, request.PostId, request.ProposalId, cancellationToken);
}
