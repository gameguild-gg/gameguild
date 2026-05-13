using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GameGuild.CQRS;
using Moq;
using System.Text.Json;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Controllers;

public class UserNotificationsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly UserNotificationsController _controller;

    public UserNotificationsControllerTests()
    {
        _controller = new UserNotificationsController(_sender.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task GetNotifications_ShouldMapQueryAndReturnOk()
    {
        var userId = Guid.NewGuid();
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;
        var result = PagedResult<UserNotificationDto>.FromPage(
            new[] { CreateNotificationDto(userId) },
            totalCount: 1,
            pageNumber: 2,
            pageSize: 15);

        _sender.Setup(sender => sender.Send(
                It.Is<GetUserNotificationsPagedQuery>(query =>
                    query.UserId == userId &&
                    query.Search == "invoice" &&
                    query.SortBy == "createdAt" &&
                    query.SortDirection == "asc" &&
                    query.IsRead == true &&
                    query.IsArchived == false &&
                    query.Type == "billing" &&
                    query.Priority == "high" &&
                    query.FromDate == fromDate &&
                    query.ToDate == toDate &&
                    query.PageNumber == 2 &&
                    query.PageSize == 15),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var action = await _controller.GetNotifications(
            userId,
            page: 2,
            pageSize: 15,
            search: "invoice",
            sortBy: "createdAt",
            sortDirection: "asc",
            isRead: true,
            isArchived: false,
            type: "billing",
            priority: "high",
            fromDate: fromDate,
            toDate: toDate,
            ct: CancellationToken.None);

        action.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(result);
    }

    [Fact]
    public async Task BulkNotificationEndpoints_ShouldMapCommandsAndReturnNoContent()
    {
        var userId = Guid.NewGuid();
        var body = new BulkNotificationRequest(new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });

        _sender.Setup(sender => sender.Send(
                It.Is<BulkMarkNotificationsAsReadCommand>(command =>
                    command.UserId == userId && command.NotificationIds.SequenceEqual(body.NotificationIds!)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sender.Setup(sender => sender.Send(
                It.Is<BulkMarkNotificationsAsUnreadCommand>(command =>
                    command.UserId == userId && command.NotificationIds.SequenceEqual(body.NotificationIds!)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sender.Setup(sender => sender.Send(
                It.Is<BulkArchiveNotificationsCommand>(command =>
                    command.UserId == userId && command.NotificationIds.SequenceEqual(body.NotificationIds!)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sender.Setup(sender => sender.Send(
                It.Is<BulkUnarchiveNotificationsCommand>(command =>
                    command.UserId == userId && command.NotificationIds.SequenceEqual(body.NotificationIds!)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var markRead = await _controller.MarkNotificationsAsRead(userId, body, CancellationToken.None);
        var markUnread = await _controller.MarkNotificationsAsUnread(userId, body, CancellationToken.None);
        var archive = await _controller.ArchiveNotifications(userId, body, CancellationToken.None);
        var unarchive = await _controller.UnarchiveNotifications(userId, body, CancellationToken.None);

        markRead.Should().BeOfType<NoContentResult>();
        markUnread.Should().BeOfType<NoContentResult>();
        archive.Should().BeOfType<NoContentResult>();
        unarchive.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task BulkNotificationEndpoints_WithEmptyIds_ShouldReturnBadRequest()
    {
        var userId = Guid.NewGuid();
        var body = new BulkNotificationRequest();

        var markRead = await _controller.MarkNotificationsAsRead(userId, body, CancellationToken.None);
        var markUnread = await _controller.MarkNotificationsAsUnread(userId, body, CancellationToken.None);
        var archive = await _controller.ArchiveNotifications(userId, body, CancellationToken.None);
        var unarchive = await _controller.UnarchiveNotifications(userId, body, CancellationToken.None);

        markRead.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().Be("NotificationIds cannot be empty");
        markUnread.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().Be("NotificationIds cannot be empty");
        archive.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().Be("NotificationIds cannot be empty");
        unarchive.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().Be("NotificationIds cannot be empty");
    }

    [Fact]
    public async Task NotificationItemEndpoints_ShouldMapQueriesCommandsAndReturnExpectedResults()
    {
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var detail = CreateNotificationDetailDto(userId, notificationId);

        _sender.SetupSequence(sender => sender.Send(
                It.Is<GetUserNotificationQuery>(query => query.UserId == userId && query.NotificationId == notificationId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail)
            .ReturnsAsync(detail);
        _sender.Setup(sender => sender.Send(
                It.Is<MarkNotificationAsReadCommand>(command => command.UserId == userId && command.NotificationId == notificationId),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sender.Setup(sender => sender.Send(
                It.Is<MarkNotificationAsUnreadCommand>(command => command.UserId == userId && command.NotificationId == notificationId),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sender.Setup(sender => sender.Send(
                It.Is<ArchiveNotificationCommand>(command => command.UserId == userId && command.NotificationId == notificationId),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sender.Setup(sender => sender.Send(
                It.Is<UnarchiveNotificationCommand>(command => command.UserId == userId && command.NotificationId == notificationId),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var check = await _controller.CheckNotificationExists(userId, notificationId, CancellationToken.None);
        var get = await _controller.GetNotification(userId, notificationId, CancellationToken.None);
        var markRead = await _controller.MarkNotificationAsRead(userId, notificationId, CancellationToken.None);
        var markUnread = await _controller.MarkNotificationAsUnread(userId, notificationId, CancellationToken.None);
        var archive = await _controller.ArchiveNotification(userId, notificationId, CancellationToken.None);
        var unarchive = await _controller.UnarchiveNotification(userId, notificationId, CancellationToken.None);

        check.Should().BeOfType<OkResult>();
        get.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(detail);
        markRead.Should().BeOfType<NoContentResult>();
        markUnread.Should().BeOfType<NoContentResult>();
        archive.Should().BeOfType<NoContentResult>();
        unarchive.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task NotificationItemEndpoints_WhenMissing_ShouldReturnNotFound()
    {
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();

        _sender.SetupSequence(sender => sender.Send(It.IsAny<GetUserNotificationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserNotificationDetailDto?)null)
            .ReturnsAsync((UserNotificationDetailDto?)null);

        var check = await _controller.CheckNotificationExists(userId, notificationId, CancellationToken.None);
        var get = await _controller.GetNotification(userId, notificationId, CancellationToken.None);

        check.Should().BeOfType<NotFoundResult>();
        get.Should().BeOfType<NotFoundResult>();
    }

    private static UserNotificationDto CreateNotificationDto(Guid userId, Guid? notificationId = null)
    {
        return new UserNotificationDto(
            notificationId ?? Guid.NewGuid(),
            userId,
            "billing",
            "Invoice Ready",
            "Invoice message",
            "high",
            "finance",
            false,
            false,
            null,
            null,
            null,
            "https://example.com/invoices/1",
            "View",
            null,
            JsonMap(new Dictionary<string, object?> { ["invoiceId"] = 42 }),
            DateTimeOffset.UtcNow,
            null,
            new byte[] { 1 });
    }

    private static UserNotificationDetailDto CreateNotificationDetailDto(Guid userId, Guid notificationId)
    {
        var notification = CreateNotificationDto(userId, notificationId);
        var related = new List<UserNotificationDto> { CreateNotificationDto(userId) };
        var actions = new List<NotificationActionDto>
        {
            new("view", "View", "https://example.com/invoices/1", "link", true)
        };

        return new UserNotificationDetailDto(notification, related, actions);
    }
}
