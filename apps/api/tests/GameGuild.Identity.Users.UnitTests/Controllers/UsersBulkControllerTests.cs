using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Controllers;

public class UsersBulkControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly UsersBulkController _controller;

    public UsersBulkControllerTests()
    {
        _controller = new UsersBulkController(_sender.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task BulkCreateUsers_ShouldMapCommandAndReturnCreated()
    {
        var request = new BulkCreateUsersRequest(
            new[]
            {
                new CreateUserRequestItem("one@example.com", "One"),
                new CreateUserRequestItem("two@example.com", "Two", "+15550002")
            });
        var response = new BulkCreateUsersResponse(
            CreatedUserIds: new[] { Guid.NewGuid(), Guid.NewGuid() },
            FailedEmails: Array.Empty<string>());

        _sender.Setup(sender => sender.Send(
                It.Is<BulkCreateUsersCommand>(command => command.Users.SequenceEqual(request.Users)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.BulkCreateUsers(request, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedResult>().Subject;
        created.Location.Should().BeEmpty();
        created.Value.Should().Be(response);
    }

    [Fact]
    public async Task BulkUpdateDeleteAndPurgeEndpoints_ShouldMapCommandsAndReturnNoContent()
    {
        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var updates = new[]
        {
            new UpdateUserRequestItem(userIds[0], "Updated One", "+15550001"),
            new UpdateUserRequestItem(userIds[1], "Updated Two")
        };
        var updateRequest = new BulkUpdateUsersRequest(updates);
        var deleteRequest = new BulkDeleteUsersRequest(userIds);
        var purgeRequest = new BulkPurgeUsersRequest(userIds, PurgeStrategy.Scheduled);

        _sender.Setup(sender => sender.Send(
                It.Is<BulkUpdateUsersCommand>(command => command.Updates.SequenceEqual(updateRequest.Updates)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sender.Setup(sender => sender.Send(
                It.Is<BulkDeleteUsersCommand>(command => command.UserIds.SequenceEqual(deleteRequest.UserIds)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sender.Setup(sender => sender.Send(
                It.Is<BulkPurgeUsersCommand>(command =>
                    command.UserIds.SequenceEqual(purgeRequest.UserIds) &&
                    command.Strategy == PurgeStrategy.Scheduled),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var partialUpdate = await _controller.BulkPartialUpdateUsers(updateRequest, CancellationToken.None);
        var fullUpdate = await _controller.BulkFullUpdateUsers(updateRequest, CancellationToken.None);
        var delete = await _controller.BulkDeleteUsers(deleteRequest, CancellationToken.None);
        var purge = await _controller.BulkPurgeUsers(purgeRequest, CancellationToken.None);

        partialUpdate.Should().BeOfType<NoContentResult>();
        fullUpdate.Should().BeOfType<NoContentResult>();
        delete.Should().BeOfType<NoContentResult>();
        purge.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task BulkAdminEndpoints_ShouldMapCommandsAndReturnOk()
    {
        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var user = CreateUserDto(userIds[0]);
        var activateRequest = new BulkActivateUsersRequest(userIds);
        var deactivateRequest = new BulkDeactivateUsersRequest(userIds);
        var suspendRequest = new BulkSuspendUsersRequest(userIds);
        var unsuspendRequest = new BulkUnsuspendUsersRequest(userIds);
        var restoreRequest = new BulkRestoreUsersRequest(userIds);
        var activateResponse = new BulkActivateUsersResponse(new[] { user }, Array.Empty<Guid>());
        var deactivateResponse = new BulkDeactivateUsersResponse(new[] { user }, Array.Empty<Guid>());
        var suspendResponse = new BulkSuspendUsersResponse(new[] { user }, Array.Empty<Guid>());
        var unsuspendResponse = new BulkUnsuspendUsersResponse(new[] { user }, Array.Empty<Guid>());
        var restoreResponse = new BulkRestoreUsersResponse(new[] { user }, Array.Empty<Guid>());

        _sender.Setup(sender => sender.Send(
                It.Is<BulkActivateUsersCommand>(command => command.UserIds.SequenceEqual(activateRequest.UserIds)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(activateResponse);
        _sender.Setup(sender => sender.Send(
                It.Is<BulkDeactivateUsersCommand>(command => command.UserIds.SequenceEqual(deactivateRequest.UserIds)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deactivateResponse);
        _sender.Setup(sender => sender.Send(
                It.Is<BulkSuspendUsersCommand>(command => command.UserIds.SequenceEqual(suspendRequest.UserIds)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspendResponse);
        _sender.Setup(sender => sender.Send(
                It.Is<BulkUnsuspendUsersCommand>(command => command.UserIds.SequenceEqual(unsuspendRequest.UserIds)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(unsuspendResponse);
        _sender.Setup(sender => sender.Send(
                It.Is<BulkRestoreUsersCommand>(command => command.UserIds.SequenceEqual(restoreRequest.UserIds)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(restoreResponse);

        var activate = await _controller.BulkActivateUsers(activateRequest, CancellationToken.None);
        var deactivate = await _controller.BulkDeactivateUsers(deactivateRequest, CancellationToken.None);
        var suspend = await _controller.BulkSuspendUsers(suspendRequest, CancellationToken.None);
        var unsuspend = await _controller.BulkUnsuspendUsers(unsuspendRequest, CancellationToken.None);
        var undelete = await _controller.BulkUndeleteUsers(restoreRequest, CancellationToken.None);

        activate.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(activateResponse);
        deactivate.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(deactivateResponse);
        suspend.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(suspendResponse);
        unsuspend.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(unsuspendResponse);
        undelete.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(restoreResponse);
    }

    [Fact]
    public async Task IndividualAdminEndpoints_ShouldMapCommandsAndReturnExpectedResults()
    {
        var userId = Guid.NewGuid();
        var user = CreateUserDto(userId);

        _sender.Setup(sender => sender.Send(
                It.Is<ActivateUserCommand>(command => command.UserId == userId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _sender.Setup(sender => sender.Send(
                It.Is<DeactivateUserCommand>(command => command.UserId == userId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _sender.Setup(sender => sender.Send(
                It.Is<SuspendUserCommand>(command => command.UserId == userId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _sender.Setup(sender => sender.Send(
                It.Is<UnsuspendUserCommand>(command => command.UserId == userId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _sender.Setup(sender => sender.Send(
                It.Is<RestoreUserCommand>(command => command.UserId == userId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _sender.Setup(sender => sender.Send(
                It.Is<PurgeUserCommand>(command => command.UserId == userId && command.Strategy == PurgeStrategy.GracePeriod),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var activate = await _controller.ActivateUser(userId, CancellationToken.None);
        var deactivate = await _controller.DeactivateUser(userId, CancellationToken.None);
        var suspend = await _controller.SuspendUser(userId, CancellationToken.None);
        var unsuspend = await _controller.UnsuspendUser(userId, CancellationToken.None);
        var undelete = await _controller.UndeleteUser(userId, CancellationToken.None);
        var purge = await _controller.PurgeUser(userId, CancellationToken.None);

        activate.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(user);
        deactivate.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(user);
        suspend.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(user);
        unsuspend.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(user);
        undelete.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(user);
        purge.Should().BeOfType<NoContentResult>();
    }

    private static UserDto CreateUserDto(Guid? id = null, string name = "Test User", string? phoneNumber = "+15550000")
        => new(id ?? Guid.NewGuid(), "user@example.com", name, DateTime.UtcNow, DateTime.UtcNow, true, phoneNumber, DateTime.UtcNow);
}
