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
    private readonly Mock<ITenantMembershipChecker> _membershipChecker = new();
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _actorContextAccessor.Setup(x => x.ActorContext).Returns(CreateActorContext(roles: new[] { "Admin" }));
        _controller = new UsersController(_sender.Object, _actorContextAccessor.Object, _membershipChecker.Object)
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

    [Theory]
    [InlineData("Owner")]
    [InlineData("TenantAdmin")]
    public async Task GetUsers_ShouldAllowTenantAdministratorsWithoutExplicitUsersReadPermission(string role)
    {
        var result = PagedResult<UserDto>.FromPage(new[] { CreateUserDto() }, totalCount: 1, pageNumber: 1, pageSize: 25);
        _actorContextAccessor.Setup(x => x.ActorContext).Returns(CreateActorContext(roles: new[] { role }));
        _sender.Setup(sender => sender.Send(It.Is<GetUsersQuery>(query => query.Limit == 25), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var action = await _controller.GetUsers(limit: 25, ct: CancellationToken.None);

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

    [Fact]
    public async Task UserByIdEndpoints_ShouldForbidRegularMemberFromManagingAnotherUser()
    {
        var actorId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        _actorContextAccessor.Setup(x => x.ActorContext)
            .Returns(CreateActorContext(["Member"], subjectId: actorId));

        (await _controller.CheckUserExistsById(targetUserId, CancellationToken.None)).Should().BeOfType<ForbidResult>();
        (await _controller.GetUserById(targetUserId, CancellationToken.None)).Should().BeOfType<ForbidResult>();
        (await _controller.PatchUserById(targetUserId, new UpdateUserRequest("Blocked", null), CancellationToken.None)).Should().BeOfType<ForbidResult>();
        (await _controller.UpdateUserById(targetUserId, new CreateUserRequest("target@example.com", "Blocked", null), CancellationToken.None)).Should().BeOfType<ForbidResult>();
        (await _controller.DeleteUserById(targetUserId, CancellationToken.None)).Should().BeOfType<ForbidResult>();

        _sender.Verify(sender => sender.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        _sender.Verify(sender => sender.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _sender.Verify(sender => sender.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UserByIdEndpoints_ShouldAllowRegularMemberToManageSelf()
    {
        var actorId = Guid.NewGuid();
        var user = CreateUserDto(actorId);
        _actorContextAccessor.Setup(x => x.ActorContext)
            .Returns(CreateActorContext(["Member"], subjectId: actorId));
        _sender.Setup(sender => sender.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _sender.Setup(sender => sender.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _sender.Setup(sender => sender.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        (await _controller.GetUserById(actorId, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await _controller.PatchUserById(actorId, new UpdateUserRequest("Self", null), CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await _controller.DeleteUserById(actorId, CancellationToken.None)).Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UserByIdEndpoints_ShouldForbidTenantAdminFromManagingUserOutsideCurrentTenant()
    {
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        _actorContextAccessor.Setup(x => x.ActorContext)
            .Returns(CreateActorContext(["TenantAdmin"], subjectId: actorId, tenantId: tenantId));

        var result = await _controller.GetUserById(targetUserId, CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        _sender.Verify(sender => sender.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UserByIdEndpoints_ShouldAllowTenantAdminToManageUserInCurrentTenant()
    {
        var tenantId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var user = CreateUserDto(targetUserId);
        _actorContextAccessor.Setup(x => x.ActorContext)
            .Returns(CreateActorContext(["TenantAdmin"], tenantId: tenantId));
        _membershipChecker
            .Setup(x => x.IsUserMemberOfTenantAsync(targetUserId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _sender.Setup(sender => sender.Send(It.Is<GetUserByIdQuery>(query => query.UserId == targetUserId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _controller.GetUserById(targetUserId, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(user);
    }

    private static UserDto CreateUserDto(Guid? id = null, string name = "Test User", string? phoneNumber = "+15550000")
        => new(id ?? Guid.NewGuid(), "user@example.com", name, DateTime.UtcNow, DateTime.UtcNow, true, phoneNumber, DateTime.UtcNow);

    private static ActorContext CreateActorContext(
        IEnumerable<string> roles,
        IEnumerable<string>? permissions = null,
        Guid? subjectId = null,
        Guid? tenantId = null)
        => new()
        {
            ActorKind = ActorKind.User,
            SubjectId = (subjectId ?? Guid.NewGuid()).ToString(),
            TenantId = tenantId,
            Roles = roles.ToHashSet(),
            Permissions = (permissions ?? Array.Empty<string>()).ToHashSet(),
            TypedAttributes = ActorAttributes.Empty,
            IsAuthenticated = true
        };
}
