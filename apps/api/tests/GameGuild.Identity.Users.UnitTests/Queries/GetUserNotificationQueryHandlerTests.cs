using FluentAssertions;
using System.Reflection;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Queries;

public class GetUserNotificationQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserNotificationRepository> _notificationRepositoryMock;
    private readonly GetUserNotificationQueryHandler _handler;

    public GetUserNotificationQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _notificationRepositoryMock = new Mock<IUserNotificationRepository>();
        _handler = new GetUserNotificationQueryHandler(_userRepositoryMock.Object, _notificationRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingUserAndNotification_ShouldReturnNotificationDetail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var relatedEntityId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);
        var query = new GetUserNotificationQuery(userId, notificationId);
        var notification = UserNotification.Create(userId, "lease", "Lease ready", "The lease is ready.", NotificationPriority.High);
        notification.Id = notificationId;
        notification.ActionUrl = "/leases/1";
        notification.RelatedEntityId = relatedEntityId;
        notification.RelatedEntityType = "lease";
        notification.Metadata = """{"source":"unit-test"}""";

        var relatedNotification = UserNotification.Create(userId, "lease", "Lease reminder", "Please review the lease.");
        relatedNotification.RelatedEntityId = relatedEntityId;

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _notificationRepositoryMock
            .Setup(x => x.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        _notificationRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserNotification> { notification, relatedNotification });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Notification.Id.Should().Be(notificationId);
        result.Notification.UserId.Should().Be(userId);
        result.Notification.Priority.Should().Be("high");
        result.Notification.Category.Should().Be("lease");
        result.Notification.Metadata.Should().ContainKey("source");
        result.RelatedNotifications.Should().ContainSingle(n => n.Id == relatedNotification.Id);
        result.Actions.Should().ContainSingle(action => action.Id == "open" && action.Url == "/leases/1" && action.IsPrimary);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var query = new GetUserNotificationQuery(userId, notificationId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNotificationForDifferentUser_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);
        var query = new GetUserNotificationQuery(userId, notificationId);
        var notification = UserNotification.Create(Guid.NewGuid(), "system", "Title", "Message");
        notification.Id = notificationId;

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _notificationRepositoryMock
            .Setup(x => x.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void PrivateNotificationMappingHelpers_ShouldCover_Action_Metadata_And_Date_Branches()
    {
        var notification = UserNotification.Create(Guid.NewGuid(), "system", "Title", "Message");

        InvokePrivateStatic<List<NotificationActionDto>>("CreateActions", notification).Should().BeEmpty();

        notification.ActionUrl = " ";
        InvokePrivateStatic<List<NotificationActionDto>>("CreateActions", notification).Should().BeEmpty();

        notification.ActionUrl = "/open";
        InvokePrivateStatic<List<NotificationActionDto>>("CreateActions", notification)
            .Should()
            .ContainSingle(action => action.Url == "/open" && action.IsPrimary);

        notification.MarkAsRead();
        notification.Archive();
        notification.Metadata = "null";

        var mapped = InvokePrivateStatic<UserNotificationDto>("MapNotification", notification);
        mapped.ReadAt.Should().NotBeNull();
        mapped.ArchivedAt.Should().NotBeNull();
        mapped.Metadata.Should().BeEmpty();

        InvokePrivateStatic<Dictionary<string, System.Text.Json.JsonElement>>("DeserializeMetadata", (string?)null)
            .Should()
            .BeEmpty();
        InvokePrivateStatic<Dictionary<string, System.Text.Json.JsonElement>>("DeserializeMetadata", " ")
            .Should()
            .BeEmpty();
        InvokePrivateStatic<Dictionary<string, System.Text.Json.JsonElement>>("DeserializeMetadata", """{"key":"value"}""")
            .Should()
            .ContainKey("key");
    }

    private static T InvokePrivateStatic<T>(string methodName, params object?[] args)
        => (T)typeof(GetUserNotificationQueryHandler)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, args)!;
}
