using FluentAssertions;
using GameGuild.Announcements.Contracts;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using GameGuild.Social.Announcements.Services;
using GameGuild.Social.Follows;
using GameGuild.Social.Follows.Services;
using GameGuild.Social.Posts;
using GameGuild.Social.Posts.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Social.Announcements.UnitTests;

public class PublicationAnnouncerHandlerTests
{
    private readonly Guid Primary = Guid.NewGuid();
    private readonly Guid Coauthor = Guid.NewGuid();
    private readonly Guid FollowerOfPrimary = Guid.NewGuid();
    private readonly Guid FollowerOfCoauthor = Guid.NewGuid();
    private readonly Guid DualFollower = Guid.NewGuid();

    private readonly Mock<IPostCrudService> PostService = new();
    private readonly Mock<IFollowerService> FollowerService = new();
    private readonly Mock<INotificationService> NotificationService = new();
    private readonly Mock<ILogger<PublicationAnnouncerHandler>> Logger = new();

    private PublicationAnnouncerHandler CreateHandler() => new(
        PostService.Object, FollowerService.Object, NotificationService.Object, Logger.Object);

    private AnnouncePublicationCommand BlogCommand(Guid? coAuthorId = null) => new()
    {
        Kind = PublicationKind.BlogPostPublished,
        ActorId = Primary,
        Title = "Designing Feed Fan-Outs",
        EntityId = Guid.NewGuid(),
        CoAuthorIds = coAuthorId is { } id ? [id] : [],
        Excerpt = "What we learned fanning out to followers.",
        CanonicalUrl = $"/blogs/ada/{Guid.NewGuid():N}",
        TenantId = null,
    };

    private static Follow Follower(Guid followerId, Guid followedId)
        => Follow.Create(followerId, followedId, "User");

    private void SetupFollowers(Guid author, params Guid[] followerIds)
        => FollowerService
            .Setup(service => service.GetFollowersAsync(author, "User", 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(followerIds.Select(id => Follower(id, author)).ToList()));

    private List<Guid> SentRecipientIds()
        => NotificationService.Invocations
            .Where(invocation => invocation.Method.Name == nameof(INotificationService.SendAsync))
            .Select(invocation => (Guid)invocation.Arguments[0]!)
            .ToList();

    private async Task<Result> HandleBlogAsync(AnnouncePublicationCommand command)
    {
        var handler = CreateHandler();
        return await handler.Handle(command, CancellationToken.None);
    }

    [Fact]
    public async Task BlogPostPublished_CreatesOneCommunityPostWithExcerptAndLink()
    {
        var command = BlogCommand();
        SetupFollowers(Primary);

        await HandleBlogAsync(command);

        PostService.Verify(service => service.CreatePostAsync(
            Primary,
            It.Is<string>(content => content.Contains(command.Title)
                && content.Contains(command.Excerpt!)
                && content.Contains(command.CanonicalUrl!)),
            PostVisibility.Public,
            It.IsAny<string?>(),
            It.IsAny<MediaType?>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BlogPostPublished_NotifiesFollowerOfPrimaryWithCanonicalUrl()
    {
        var command = BlogCommand();
        SetupFollowers(Primary, FollowerOfPrimary);

        await HandleBlogAsync(command);

        NotificationService.Verify(service => service.SendAsync(
            FollowerOfPrimary,
            It.IsAny<NotificationType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<NotificationChannel>(),
            It.IsAny<Guid?>(),
            command.CanonicalUrl,
            It.IsAny<NotificationPriority>(),
            command.EntityId,
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BlogPostPublished_NotifiesFollowerOfCoauthor()
    {
        var command = BlogCommand(Coauthor);
        SetupFollowers(Primary);
        SetupFollowers(Coauthor, FollowerOfCoauthor);

        await HandleBlogAsync(command);

        SentRecipientIds().Should().Contain(FollowerOfCoauthor);
    }

    [Fact]
    public async Task BlogPostPublished_DualFollowerReceivesExactlyOneNotification()
    {
        var command = BlogCommand(Coauthor);
        SetupFollowers(Primary, FollowerOfPrimary, DualFollower);
        SetupFollowers(Coauthor, FollowerOfCoauthor, DualFollower);

        await HandleBlogAsync(command);

        var recipients = SentRecipientIds();
        recipients.Count(id => id == DualFollower).Should().Be(1);
    }

    [Fact]
    public async Task BlogPostPublished_AuthorsFollowingEachOtherAreExcluded()
    {
        var command = BlogCommand(Coauthor);
        SetupFollowers(Primary, Coauthor);
        SetupFollowers(Coauthor, Primary);

        await HandleBlogAsync(command);

        var recipients = SentRecipientIds();
        recipients.Should().NotContain(Primary);
        recipients.Should().NotContain(Coauthor);
    }

    [Fact]
    public async Task BlogPostPublished_NotificationFailureDoesNotPropagate()
    {
        var command = BlogCommand();
        SetupFollowers(Primary, FollowerOfPrimary);
        NotificationService
            .Setup(service => service.SendAsync(
                It.IsAny<Guid?>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<NotificationChannel>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<NotificationPriority>(), It.IsAny<Guid?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("notification sink down"));

        var result = await HandleBlogAsync(command);

        result.IsSuccess.Should().BeTrue();
        Logger.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("failed", StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task CoursePublished_Regression_ContentShapeUnchanged()
    {
        var command = new AnnouncePublicationCommand
        {
            Kind = PublicationKind.CoursePublished,
            ActorId = Primary,
            Title = "Intro to Shaders",
            EntityId = Guid.NewGuid(),
        };
        SetupFollowers(Primary, FollowerOfPrimary);

        await HandleBlogAsync(command);

        PostService.Verify(service => service.CreatePostAsync(
            Primary,
            $"📚 Just published a new course: {command.Title}! Check it out and enroll: /courses/{command.EntityId}",
            PostVisibility.Public,
            It.IsAny<string?>(),
            It.IsAny<MediaType?>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        NotificationService.Verify(service => service.SendAsync(
            FollowerOfPrimary,
            It.IsAny<NotificationType>(),
            "Course published",
            $"'{command.Title}' is now live.",
            It.IsAny<NotificationChannel>(),
            It.IsAny<Guid?>(),
            $"/courses/{command.EntityId}",
            It.IsAny<NotificationPriority>(),
            command.EntityId,
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
