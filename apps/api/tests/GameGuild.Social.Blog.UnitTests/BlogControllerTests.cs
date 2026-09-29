using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Social.Blog.Commands;
using GameGuild.Social.Blog.Controllers;
using GameGuild.Social.Blog.Queries;
using GameGuild.Social.Blog.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.Social.Blog.UnitTests;

public sealed class BlogControllerTests
{
    // ── BlogAuthoringController ──

    [Fact]
    public async Task Create_SendsCommandWithoutActorAndReturns201()
    {
        var post = Post(Guid.NewGuid());
        var sender = Sender();
        sender.Setup(s => s.Send(
                It.Is<CreateBlogPostCommand>(c => c.Title == "Title" && c.Format == BlogContentFormat.Markdown),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);
        var controller = new BlogAuthoringController(sender.Object);

        var result = await controller.Create(new CreateBlogPostRequest("Title", BlogContentFormat.Markdown), default);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(BlogAuthoringController.GetById));
        created.RouteValues!["id"].Should().Be(post.Id);
        sender.VerifyAll();
    }

    [Fact]
    public async Task Update_StaleRevision_MapsTo409ProblemDetailsWithRevisions()
    {
        var sender = Sender();
        sender.Setup(s => s.Send(It.Is<UpdateBlogPostDraftCommand>(c => c.PostId == PostId && c.Revision == 3), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BlogRevisionConflictException(3, 7));
        var controller = new BlogAuthoringController(sender.Object);

        var result = await controller.Update(PostId, new UpdateBlogPostDraftRequest(3, Title: "New"), default);

        var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
        var problem = conflict.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(409);
        problem.Extensions["expectedRevision"].Should().Be(3);
        problem.Extensions["currentRevision"].Should().Be(7);
    }

    [Fact]
    public async Task Publish_CoauthorDenied_MapsTo403()
    {
        var sender = Sender();
        sender.Setup(s => s.Send(new PublishBlogPostCommand(PostId), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BlogAccessDeniedException("Only the primary author can publish."));
        var controller = new BlogAuthoringController(sender.Object);

        var result = await controller.Publish(PostId, default);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Publish_UnknownPost_MapsTo404()
    {
        var sender = Sender();
        sender.Setup(s => s.Send(new PublishBlogPostCommand(PostId), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("missing"));
        var controller = new BlogAuthoringController(sender.Object);

        var result = await controller.Publish(PostId, default);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task TransferPrimary_NonCoauthorTarget_MapsTo400()
    {
        var sender = Sender();
        sender.Setup(s => s.Send(new TransferBlogPrimaryCommand(PostId, UserId), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("The new primary must be a current co-author."));
        var controller = new BlogAuthoringController(sender.Object);

        var result = await controller.TransferPrimary(PostId, new TransferBlogPrimaryRequest(UserId), default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── BlogCommentsController ──

    [Fact]
    public async Task AddComment_BlockDenied_MapsTo403()
    {
        var sender = Sender();
        sender.Setup(s => s.Send(It.IsAny<AddBlogCommentCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BlogAccessDeniedException("Comments are not allowed between blocked users."));
        var controller = new BlogCommentsController(sender.Object);

        var result = await controller.Add(PostId, new AddBlogCommentRequest("hi"), default);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task AddComment_Depth2OrDisabled_MapsTo400()
    {
        var sender = Sender();
        sender.Setup(s => s.Send(It.IsAny<AddBlogCommentCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Comment nesting is limited to one reply level."));
        var controller = new BlogCommentsController(sender.Object);

        var result = await controller.Add(PostId, new AddBlogCommentRequest("hi"), default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteComment_NonModerator_MapsTo403()
    {
        var sender = Sender();
        sender.Setup(s => s.Send(new DeleteBlogCommentCommand(CommentId), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BlogAccessDeniedException("no"));
        var controller = new BlogCommentsController(sender.Object);

        var result = await controller.Delete(CommentId, default);

        result.Should().BeOfType<ForbidResult>();
    }

    // ── BlogPublicController ──

    [Fact]
    public async Task GetPostDetail_Null_MapsTo404()
    {
        var sender = Sender();
        sender.Setup(s => s.Send(new GetPublicBlogPostDetailQuery("handle", "slug"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlogPostDetailDto?)null);
        var controller = PublicController(sender, views: null);

        var result = await controller.GetPostDetail("handle", "slug", default);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ResolveRoute_Found_ReturnsHandleAndSlug()
    {
        var resolution = new BlogRouteResolutionDto("newhandle", "new-slug");
        var sender = Sender();
        sender.Setup(s => s.Send(new ResolveBlogRouteForRedirectQuery("old", "old-slug"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolution);
        var controller = PublicController(sender, views: null);

        var result = await controller.ResolveRoute("old", "old-slug", default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(resolution);
    }

    [Fact]
    public async Task GetComments_UnpublishedPost_MapsTo404()
    {
        var sender = Sender();
        sender.Setup(s => s.Send(new IsBlogPostPublishedQuery(PostId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = PublicController(sender, views: null);

        var result = await controller.GetComments(PostId, null, null, default);

        result.Should().BeOfType<NotFoundResult>();
        sender.Verify(s => s.Send(It.IsAny<ListBlogCommentsQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetComments_AnonymousViewer_PassesNullViewerId()
    {
        var expected = new BlogCommentPage([], false);
        var sender = Sender();
        sender.Setup(s => s.Send(new IsBlogPostPublishedQuery(PostId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        sender.Setup(s => s.Send(
                It.Is<ListBlogCommentsQuery>(q => q.PostId == PostId && q.ViewerId == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = PublicController(sender, views: null, authenticated: false);

        var result = await controller.GetComments(PostId, null, null, default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(expected);
        sender.VerifyAll();
    }

    [Fact]
    public async Task GetComments_AuthenticatedViewer_PassesActorViewerId()
    {
        var sender = Sender();
        sender.Setup(s => s.Send(new IsBlogPostPublishedQuery(PostId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        sender.Setup(s => s.Send(
                It.Is<ListBlogCommentsQuery>(q => q.ViewerId == UserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlogCommentPage([], false));
        var controller = PublicController(sender, views: null, authenticated: true);

        await controller.GetComments(PostId, null, null, default);

        sender.VerifyAll();
    }

    [Fact]
    public async Task RecordView_Published_MapsTo204()
    {
        var views = new Mock<IBlogViewCounterService>();
        views.Setup(v => v.IncrementAsync(PostId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var controller = PublicController(Sender(), views.Object);

        var result = await controller.RecordView(PostId, default);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RecordView_UnknownOrUnpublished_MapsTo404()
    {
        var views = new Mock<IBlogViewCounterService>();
        views.Setup(v => v.IncrementAsync(PostId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var controller = PublicController(Sender(), views.Object);

        var result = await controller.RecordView(PostId, default);

        result.Should().BeOfType<NotFoundResult>();
    }

    private static readonly Guid PostId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CommentId = Guid.NewGuid();

    private static Mock<ISender> Sender() => new();

    private static BlogPublicController PublicController(
        Mock<ISender> sender,
        IBlogViewCounterService? views,
        bool authenticated = false)
    {
        var accessor = new ActorContextAccessor();
        accessor.SetActorContext(authenticated
            ? ActorContextBuilder.ForUser(UserId).WithTenantId(Guid.NewGuid()).Build()
            : ActorContext.Anonymous);
        return new BlogPublicController(sender.Object, accessor, views ?? Mock.Of<IBlogViewCounterService>());
    }

    private static BlogPost Post(Guid id) => BlogPost.Create(Guid.NewGuid(), "Title", "slug", BlogContentFormat.Markdown);
}
