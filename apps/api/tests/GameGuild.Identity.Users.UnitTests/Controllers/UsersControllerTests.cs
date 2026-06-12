using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly Mock<IActorContextAccessor> _actorContextAccessor = new();
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _actorContextAccessor.Setup(x => x.ActorContext).Returns(CreateActorContext(roles: new[] { "Admin" }));
        _controller = new UsersController(_sender.Object, _actorContextAccessor.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task CreateUser_ShouldMapCommandAndReturnCreatedAtAction()
    {
        var body = new CreateUserRequest("user@example.com", "Test User", "+15550000");
        var createdUser = CreateUserDto();

        _sender.Setup(sender => sender.Send(
                It.Is<CreateUserCommand>(command =>
                    command.Email == body.Email &&
                    command.Name == body.Name &&
                    command.PhoneNumber == body.PhoneNumber),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        var result = await _controller.CreateUser(body, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(UsersController.GetUserById));
        created.RouteValues!["userId"].Should().Be(createdUser.Id);
        created.Value.Should().Be(createdUser);
    }

    [Fact]
    public async Task GetUsers_ShouldMapQueryAndReturnOk()
    {
        var result = PagedResult<UserDto>.FromPage(new[] { CreateUserDto() }, totalCount: 1, pageNumber: 1, pageSize: 10);

        _sender.Setup(sender => sender.Send(
                It.Is<GetUsersQuery>(query =>
                    query.Email == "user@example.com" &&
                    query.Status == "active" &&
                    query.IncludeDeleted &&
                    query.SearchTerm == "john" &&
                    query.Cursor == "next-cursor" &&
                    query.Limit == 10 &&
                    query.Sort == "-createdAt"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var action = await _controller.GetUsers(
            email: "user@example.com",
            status: "active",
            includeDeleted: true,
            q: "john",
            cursor: "next-cursor",
            limit: 10,
            sort: "-createdAt",
            ct: CancellationToken.None);

        action.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(result);
    }

    [Fact]
    public async Task GetUsers_ShouldAllowConsoleSystemAdminWithoutExplicitUsersReadPermission()
    {
        var result = PagedResult<UserDto>.FromPage(new[] { CreateUserDto() }, totalCount: 1, pageNumber: 1, pageSize: 50);
        _actorContextAccessor.Setup(x => x.ActorContext).Returns(CreateActorContext(roles: new[] { "SystemAdmin" }));
        _sender.Setup(sender => sender.Send(It.Is<GetUsersQuery>(query => query.Limit == 50), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var action = await _controller.GetUsers(limit: 50, ct: CancellationToken.None);

        action.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(result);
    }

    [Fact]
    public async Task GetUsers_ShouldForbidActorWithoutUsersReadPermission()
    {
        _actorContextAccessor.Setup(x => x.ActorContext).Returns(CreateActorContext(roles: new[] { "Member" }));

        var action = await _controller.GetUsers(ct: CancellationToken.None);

        action.Should().BeOfType<ForbidResult>();
        _sender.Verify(sender => sender.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetUsers_ShouldAllowActorWithUsersReadPermission()
    {
        var result = PagedResult<UserDto>.FromPage(new[] { CreateUserDto() }, totalCount: 1, pageNumber: 1, pageSize: 20);
        _actorContextAccessor.Setup(x => x.ActorContext)
            .Returns(CreateActorContext(roles: new[] { "Member" }, permissions: new[] { UsersPermission.Keys.Read }));
        _sender.Setup(sender => sender.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var action = await _controller.GetUsers(ct: CancellationToken.None);

        action.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(result);
    }

    [Fact]
    public async Task GetUsers_ShouldAllowTenantOwnerWithoutExplicitUsersReadPermission()
    {
        var result = PagedResult<UserDto>.FromPage(new[] { CreateUserDto() }, totalCount: 1, pageNumber: 1, pageSize: 20);
        _actorContextAccessor.Setup(x => x.ActorContext)
            .Returns(CreateActorContext(roles: new[] { "Owner" }));
        _sender.Setup(sender => sender.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var action = await _controller.GetUsers(ct: CancellationToken.None);

        action.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(result);
    }

    [Fact]
    public async Task GetUsers_ShouldAllowTenantAdminWithoutExplicitUsersReadPermission()
    {
        var result = PagedResult<UserDto>.FromPage(new[] { CreateUserDto() }, totalCount: 1, pageNumber: 1, pageSize: 20);
        _actorContextAccessor.Setup(x => x.ActorContext)
            .Returns(CreateActorContext(roles: new[] { "TenantAdmin" }));
        _sender.Setup(sender => sender.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var action = await _controller.GetUsers(ct: CancellationToken.None);

        action.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(result);
    }

    [Fact]
    public async Task UserByIdReadEndpoints_ShouldReturnOkAndNotFound()
    {
        var existingUserId = Guid.NewGuid();
        var missingUserId = Guid.NewGuid();
        var user = CreateUserDto(existingUserId);

        _sender.Setup(sender => sender.Send(
                It.Is<GetUserByIdQuery>(query => query.UserId == existingUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _sender.Setup(sender => sender.Send(
                It.Is<GetUserByIdQuery>(query => query.UserId == missingUserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserDto?)null);

        var checkExisting = await _controller.CheckUserExistsById(existingUserId, CancellationToken.None);
        var getExisting = await _controller.GetUserById(existingUserId, CancellationToken.None);
        var checkMissing = await _controller.CheckUserExistsById(missingUserId, CancellationToken.None);
        var getMissing = await _controller.GetUserById(missingUserId, CancellationToken.None);

        checkExisting.Should().BeOfType<OkResult>();
        getExisting.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(user);
        checkMissing.Should().BeOfType<NotFoundResult>();
        getMissing.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateAndDeleteEndpoints_ShouldMapCommandsAndReturnExpectedResults()
    {
        var userId = Guid.NewGuid();
        var patchBody = new UpdateUserRequest("Updated Name", "+15550123");
        var putBody = new CreateUserRequest("ignored@example.com", "Updated Name", "+15550123");
        var updatedUser = CreateUserDto(userId, "Updated Name", "+15550123");

        _sender.Setup(sender => sender.Send(
                It.Is<UpdateUserCommand>(command =>
                    command.UserId == userId &&
                    command.Name == patchBody.Name &&
                    command.PhoneNumber == patchBody.PhoneNumber),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedUser);
        _sender.Setup(sender => sender.Send(
                It.Is<UpdateUserCommand>(command =>
                    command.UserId == userId &&
                    command.Name == putBody.Name &&
                    command.PhoneNumber == putBody.PhoneNumber),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedUser);
        _sender.Setup(sender => sender.Send(
                It.Is<DeleteUserCommand>(command => command.UserId == userId),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var patchResult = await _controller.PatchUserById(userId, patchBody, CancellationToken.None);
        var updateResult = await _controller.UpdateUserById(userId, putBody, CancellationToken.None);
        var deleteResult = await _controller.DeleteUserById(userId, CancellationToken.None);

        patchResult.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(updatedUser);
        updateResult.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(updatedUser);
        deleteResult.Should().BeOfType<NoContentResult>();
    }

    private static UserDto CreateUserDto(Guid? id = null, string name = "Test User", string? phoneNumber = "+15550000")
        => new(id ?? Guid.NewGuid(), "user@example.com", name, DateTime.UtcNow, DateTime.UtcNow, true, phoneNumber, DateTime.UtcNow);

    private static ActorContext CreateActorContext(IEnumerable<string> roles, IEnumerable<string>? permissions = null)
        => new()
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = null,
            Roles = roles.ToHashSet(),
            Permissions = (permissions ?? Array.Empty<string>()).ToHashSet(),
            TypedAttributes = ActorAttributes.Empty,
            IsAuthenticated = true
        };
}
