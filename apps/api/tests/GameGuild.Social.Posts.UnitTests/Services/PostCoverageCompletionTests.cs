using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Social.Posts.Controllers;
using GameGuild.Social.Posts.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Social.Posts.Tests.Services;

public class PostCoverageCompletionTests
{
    [Fact]
    public async Task ContentReferenceRemove_WhenReferenceMissing_ShouldReturnReferenceNotFound()
    {
        var context = new Mock<IApplicationDbContext>();
        context.Setup(x => x.Set<PostContentReference>())
            .Returns(new List<PostContentReference>().AsQueryable().BuildMockDbSet().Object);
        var service = new PostContentReferenceService(context.Object, NullLogger<PostContentReferenceService>.Instance);

        var result = await service.RemoveContentReferenceAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ContentReference.NotFound");
    }

    [Fact]
    public async Task ContentReferenceAdd_WhenPostMissing_ShouldReturnPostNotFound()
    {
        var context = new Mock<IApplicationDbContext>();
        context.Setup(x => x.Set<Post>())
            .Returns(new List<Post>().AsQueryable().BuildMockDbSet().Object);
        context.Setup(x => x.Set<PostContentReference>())
            .Returns(new List<PostContentReference>().AsQueryable().BuildMockDbSet().Object);
        var service = new PostContentReferenceService(context.Object, NullLogger<PostContentReferenceService>.Instance);

        var result = await service.AddContentReferenceAsync(Guid.NewGuid(), Guid.NewGuid(), "Course");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Post.NotFound");
    }

    [Fact]
    public async Task TagAdd_WhenPostMissing_ShouldReturnPostNotFound()
    {
        var context = new Mock<IApplicationDbContext>();
        context.Setup(x => x.Set<Post>())
            .Returns(new List<Post>().AsQueryable().BuildMockDbSet().Object);
        context.Setup(x => x.Set<PostTag>())
            .Returns(new List<PostTag>().AsQueryable().BuildMockDbSet().Object);
        context.Setup(x => x.Set<PostTagAssignment>())
            .Returns(new List<PostTagAssignment>().AsQueryable().BuildMockDbSet().Object);
        var service = new PostTagService(context.Object, NullLogger<PostTagService>.Instance);

        var result = await service.AddTagsToPostAsync(Guid.NewGuid(), ["missing"]);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Post.NotFound");
    }

    [Fact]
    public async Task EngagementUnfollow_WhenFollowerMissing_ShouldReturnNotFollowing()
    {
        var context = new Mock<IApplicationDbContext>();
        context.Setup(x => x.Set<PostFollower>())
            .Returns(new List<PostFollower>().AsQueryable().BuildMockDbSet().Object);
        var service = new PostEngagementService(context.Object, NullLogger<PostEngagementService>.Instance);

        var result = await service.UnfollowPostAsync(Guid.NewGuid(), Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PostFollower.NotFound");
    }

    [Fact]
    public async Task Controllers_WithNullActor_ShouldReturnUnauthorizedForProtectedActions()
    {
        var postService = new Mock<IPostService>();
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor.SetupGet(x => x.ActorContext).Returns((ActorContext)null!);

        var comments = new PostCommentsController(postService.Object, actorAccessor.Object);
        var interactions = new PostInteractionsController(postService.Object, actorAccessor.Object);
        var crud = new PostsCrudController(postService.Object, actorAccessor.Object);

        var addComment = await comments.AddComment(Guid.NewGuid(), new AddCommentRequest { Content = "comment" });
        var toggleLike = await interactions.ToggleLike(Guid.NewGuid());
        var createPost = await crud.CreatePost(new CreatePostRequest { Content = "post" });

        addComment.Should().BeOfType<UnauthorizedResult>();
        toggleLike.Should().BeOfType<UnauthorizedResult>();
        createPost.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Controllers_WithActorWithoutGuidSubject_ShouldReturnUnauthorizedForProtectedActions()
    {
        var postService = new Mock<IPostService>();
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor.SetupGet(x => x.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = "not-a-guid",
            TenantId = null,
            IsAuthenticated = true,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>()
        });

        var comments = new PostCommentsController(postService.Object, actorAccessor.Object);
        var interactions = new PostInteractionsController(postService.Object, actorAccessor.Object);
        var crud = new PostsCrudController(postService.Object, actorAccessor.Object);

        var addComment = await comments.AddComment(Guid.NewGuid(), new AddCommentRequest { Content = "comment" });
        var toggleLike = await interactions.ToggleLike(Guid.NewGuid());
        var createPost = await crud.CreatePost(new CreatePostRequest { Content = "post" });

        addComment.Should().BeOfType<UnauthorizedResult>();
        toggleLike.Should().BeOfType<UnauthorizedResult>();
        createPost.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Controllers_WithGuidActor_ShouldExecuteProtectedActions()
    {
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var postService = new Mock<IPostService>();
        postService.Setup(x => x.AddCommentAsync(postId, userId, "comment", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(PostComment.Create(postId, userId, "comment")));
        postService.Setup(x => x.TogglePostLikeAsync(postId, userId, "like", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
        postService.Setup(x => x.CreatePostAsync(userId, "post", PostVisibility.Public, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Post.Create(userId, "post")));

        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor.SetupGet(x => x.ActorContext).Returns(ActorContextBuilder.ForUser(userId).Build());

        var comments = new PostCommentsController(postService.Object, actorAccessor.Object);
        var interactions = new PostInteractionsController(postService.Object, actorAccessor.Object);
        var crud = new PostsCrudController(postService.Object, actorAccessor.Object);

        var addComment = await comments.AddComment(postId, new AddCommentRequest { Content = "comment" });
        var toggleLike = await interactions.ToggleLike(postId);
        var createPost = await crud.CreatePost(new CreatePostRequest { Content = "post" });

        addComment.Should().BeOfType<CreatedResult>();
        toggleLike.Should().BeOfType<OkObjectResult>();
        createPost.Should().BeOfType<CreatedAtActionResult>();
    }
}
