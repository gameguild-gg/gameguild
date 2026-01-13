using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Users;
using GameGuild.Resources;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class DeleteUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly Mock<IResourceQuotaService> _quotaServiceMock;
    private readonly Mock<IActorContextAccessor> _actorContextAccessorMock;
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _publisherMock = new Mock<IPublisher>();
        _quotaServiceMock = new Mock<IResourceQuotaService>();
        _actorContextAccessorMock = new Mock<IActorContextAccessor>();
        _actorContextAccessorMock.Setup(x => x.ActorContext).Returns(ActorContext.Anonymous);
        _handler = new DeleteUserCommandHandler(
            _userRepositoryMock.Object,
            _publisherMock.Object,
            _quotaServiceMock.Object,
            _actorContextAccessorMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldDeleteUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", Name = "Test User" };
        var command = new DeleteUserCommand(userId);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _userRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new DeleteUserCommand(userId);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteUser_DecrementsQuota_WhenUserDeleted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", Name = "Test User" };
        user.SetProperties(new Dictionary<string, object?> { ["TenantId"] = tenantId });
        var command = new DeleteUserCommand(userId);

        var actorContext = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = userId.ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            IsAuthenticated = true
        };
        _actorContextAccessorMock.Setup(x => x.ActorContext).Returns(actorContext);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _quotaServiceMock
            .Setup(x => x.DecrementUsageAsync(tenantId, ResourceUsageType.Users, 1L, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _userRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _quotaServiceMock.Verify(
            x => x.DecrementUsageAsync(tenantId, ResourceUsageType.Users, 1L, null, null, It.IsAny<CancellationToken>()),
            Times.Once,
            "quota should be decremented when user is deleted");
    }
}
