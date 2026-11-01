using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Social.Blog.Commands;
using GameGuild.Social.Blog.Controllers;
using GameGuild.Social.Blog.Queries;
using GameGuild.Social.Blog.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.Social.Blog.UnitTests;

/// <summary>
/// SystemAdmin admin controller (issue #96): command dispatch and the error mapping
/// (403/404/400) shared with the sibling blog controllers.
/// </summary>
public sealed class BlogAdminControllerTests
{
    private static Guid PostId { get; } = Guid.NewGuid();

    private static BlogPost Post(Guid id) => BlogPost.Create(Guid.NewGuid(), "Title", "slug-" + id, BlogContentFormat.Markdown);

    private static Mock<ISender> Sender() => new();

    [Fact]
    public async Task Unpublish_ReturnsPost()
    {
        var post = Post(PostId);
        var sender = Sender();
        sender.Setup(s => s.Send(new AdminUnpublishBlogPostCommand(PostId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(post);
        var controller = new BlogAdminController(sender.Object);

        var result = await controller.Unpublish(PostId, default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(post);
    }

    [Fact]
    public async Task Unpublish_Denied_MapsTo403()
    {
        var sender = Sender();
        sender.Setup(s => s.Send(It.IsAny<AdminUnpublishBlogPostCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("SystemAdmin required"));
        var controller = new BlogAdminController(sender.Object);

        (await controller.Unpublish(PostId, default)).Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Unpublish_Unknown_MapsTo404()
    {
        var sender = Sender();
        sender.Setup(s => s.Send(It.IsAny<AdminUnpublishBlogPostCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("missing"));
        var controller = new BlogAdminController(sender.Object);

        (await controller.Unpublish(PostId, default)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeletePost_Returns204()
    {
        var sender = Sender();
        sender.Setup(s => s.Send(new AdminDeleteBlogPostCommand(PostId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GameGuild.CQRS.Unit.Value);
        var controller = new BlogAdminController(sender.Object);

        (await controller.DeletePost(PostId, default)).Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteComment_Returns204()
    {
        var commentId = Guid.NewGuid();
        var sender = Sender();
        sender.Setup(s => s.Send(new AdminDeleteBlogCommentCommand(commentId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GameGuild.CQRS.Unit.Value);
        var controller = new BlogAdminController(sender.Object);

        (await controller.DeleteComment(commentId, default)).Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ImportGhost_ReturnsReport()
    {
        var report = GhostImportReport.Empty(dryRun: true);
        var sender = Sender();
        sender.Setup(s => s.Send(
                It.Is<ImportGhostContentCommand>(c => c.DryRun && c.ExportJson == "{}"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
        var controller = new BlogAdminController(sender.Object);

        var result = await controller.ImportGhost(new GhostImportRequest("{}", DryRun: true), default);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(report);
    }

    [Fact]
    public async Task ImportGhost_MalformedJson_MapsTo400()
    {
        var sender = Sender();
        sender.Setup(s => s.Send(It.IsAny<ImportGhostContentCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Text.Json.JsonException("bad payload"));
        var controller = new BlogAdminController(sender.Object);

        var result = await controller.ImportGhost(new GhostImportRequest("not json", DryRun: true), default);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be("GHOST_IMPORT_INVALID");
    }

    [Fact]
    public async Task ImportGhost_Denied_MapsTo403()
    {
        var sender = Sender();
        sender.Setup(s => s.Send(It.IsAny<ImportGhostContentCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("SystemAdmin required"));
        var controller = new BlogAdminController(sender.Object);

        (await controller.ImportGhost(new GhostImportRequest("{}", DryRun: true), default))
            .Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Queries_DispatchAndReturnPage()
    {
        var sender = Sender();
        sender.Setup(s => s.Send(new ListBlogAdminPostsQuery(2, null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlogAdminPostPage([], false));
        sender.Setup(s => s.Send(new ListBlogAdminCommentsQuery(0), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlogAdminCommentPage([], false));
        sender.Setup(s => s.Send(new GetBlogAdminStatsQuery(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BlogAdminStatsDto(1, 2, 3));
        var controller = new BlogAdminController(sender.Object);

        (await controller.Posts(page: 2, status: null, default)).Should().BeOfType<OkObjectResult>();
        (await controller.Comments(0, default)).Should().BeOfType<OkObjectResult>();
        (await controller.Stats(default)).Should().BeOfType<OkObjectResult>();
    }
}
