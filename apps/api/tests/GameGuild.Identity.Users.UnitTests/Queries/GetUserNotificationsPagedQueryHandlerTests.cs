using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Queries;

public class GetUserNotificationsPagedQueryHandlerTests
{
    private readonly Mock<IUserNotificationRepository> _notificationRepositoryMock;
    private readonly GetUserNotificationsPagedQueryHandler _handler;

    public GetUserNotificationsPagedQueryHandlerTests()
    {
        _notificationRepositoryMock = new Mock<IUserNotificationRepository>();
        _handler = new GetUserNotificationsPagedQueryHandler(_notificationRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldPassFiltersToRepository_AndMapNotifications()
    {
        var userId = Guid.NewGuid();
        var fromDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var toDate = new DateTimeOffset(2024, 1, 31, 23, 59, 59, TimeSpan.Zero);
        var createdAt = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2024, 1, 16, 13, 30, 0, DateTimeKind.Utc);
        var readAt = new DateTime(2024, 1, 17, 9, 0, 0, DateTimeKind.Utc);
        var archivedAt = new DateTime(2024, 1, 18, 10, 15, 0, DateTimeKind.Utc);
        var notificationWithMetadata = UserNotification.Create(userId, "billing", "Invoice Ready", "Your invoice is ready", NotificationPriority.High);
        notificationWithMetadata.Id = Guid.NewGuid();
        notificationWithMetadata.IsRead = true;
        notificationWithMetadata.IsArchived = true;
        notificationWithMetadata.ReadAt = readAt;
        notificationWithMetadata.ArchivedAt = archivedAt;
        notificationWithMetadata.ActionUrl = "https://example.com/invoices/1";
        notificationWithMetadata.Metadata = "{\"invoiceId\":42}";
        notificationWithMetadata.CreatedAt = createdAt;
        notificationWithMetadata.UpdatedAt = updatedAt;
        notificationWithMetadata.Version = 7;

        var notificationWithoutMetadata = UserNotification.Create(userId, "system", "Welcome", "Hello there");
        notificationWithoutMetadata.Id = Guid.NewGuid();
        notificationWithoutMetadata.CreatedAt = createdAt.AddDays(1);
        notificationWithoutMetadata.UpdatedAt = updatedAt.AddDays(1);
        notificationWithoutMetadata.Version = 3;

        var query = new GetUserNotificationsPagedQuery(
            userId,
            Search: "invoice",
            SortBy: "priority",
            SortDirection: "asc",
            IsRead: true,
            IsArchived: false,
            Type: "billing",
            Priority: "high",
            FromDate: fromDate,
            ToDate: toDate,
            PageNumber: 2,
            PageSize: 15);

        _notificationRepositoryMock
            .Setup(x => x.GetPagedByUserIdAsync(
                userId,
                2,
                15,
                "invoice",
                "priority",
                "asc",
                false,
                "billing",
                true,
                "high",
                It.Is<DateTime?>(value => value == fromDate.DateTime),
                It.Is<DateTime?>(value => value == toDate.DateTime),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserNotification> { notificationWithMetadata, notificationWithoutMetadata }, 2));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(15);
        result.Items.Should().HaveCount(2);

        result.Items[0].Id.Should().Be(notificationWithMetadata.Id);
        result.Items[0].Type.Should().Be("billing");
        result.Items[0].Priority.Should().Be("high");
        result.Items[0].IsRead.Should().BeTrue();
        result.Items[0].IsArchived.Should().BeTrue();
        result.Items[0].ReadAt.Should().Be(new DateTimeOffset(readAt, TimeSpan.Zero));
        result.Items[0].ArchivedAt.Should().Be(new DateTimeOffset(archivedAt, TimeSpan.Zero));
        result.Items[0].ActionUrl.Should().Be("https://example.com/invoices/1");
        result.Items[0].Metadata["invoiceId"].GetInt32().Should().Be(42);
        result.Items[0].UpdatedAt.Should().Be(new DateTimeOffset(updatedAt, TimeSpan.Zero));
        result.Items[0].Version.Should().BeEquivalentTo(BitConverter.GetBytes(7));

        result.Items[1].Id.Should().Be(notificationWithoutMetadata.Id);
        result.Items[1].Metadata.Should().BeEmpty();
        result.Items[1].Priority.Should().Be("normal");
    }

    [Fact]
    public async Task Handle_WhenMetadataDeserializesToNull_ShouldMapEmptyMetadataDictionary()
    {
        var userId = Guid.NewGuid();
        var notification = UserNotification.Create(userId, "system", "Null Metadata", "Payload");
        notification.Id = Guid.NewGuid();
        notification.Metadata = "null";
        notification.CreatedAt = new DateTime(2024, 2, 1, 12, 0, 0, DateTimeKind.Utc);
        notification.UpdatedAt = new DateTime(2024, 2, 1, 13, 0, 0, DateTimeKind.Utc);
        notification.Version = 5;

        _notificationRepositoryMock
            .Setup(x => x.GetPagedByUserIdAsync(
                userId,
                1,
                20,
                null,
                null,
                "desc",
                null,
                null,
                null,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserNotification> { notification }, 1));

        var result = await _handler.Handle(new GetUserNotificationsPagedQuery(userId), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Metadata.Should().BeEmpty();
    }
}
