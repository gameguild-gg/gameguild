using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Resources;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class BulkDeleteUsersCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IResourceQuotaService> _quotaServiceMock;
    private readonly Mock<IActorContextAccessor> _actorContextAccessorMock;
    private readonly BulkDeleteUsersCommandHandler _handler;

    public BulkDeleteUsersCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _quotaServiceMock = new Mock<IResourceQuotaService>();
        _actorContextAccessorMock = new Mock<IActorContextAccessor>();
        _actorContextAccessorMock.Setup(x => x.ActorContext).Returns(ActorContext.Anonymous);
        _handler = new BulkDeleteUsersCommandHandler(
            _userRepositoryMock.Object,
            _quotaServiceMock.Object,
            _actorContextAccessorMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidUserIds_ShouldDeleteAllUsers()
    {
        // Arrange
        var user1 = User.Create("user1@test.com", "User One", null);
        var user2 = User.Create("user2@test.com", "User Two", null);
        var user3 = User.Create("user3@test.com", "User Three", null);
        var users = new List<User> { user1, user2, user3 };

        var userIds = users.Select(u => u.Id).ToList();
        var command = new BulkDeleteUsersCommand(userIds);

        _userRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        _userRepositoryMock
            .Setup(x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        _userRepositoryMock.Verify(
            x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task Handle_WithNonExistentUsers_ShouldOnlyDeleteExistingUsers()
    {
        // Arrange
        var user1 = User.Create("user1@test.com", "User One", null);
        var existingUsers = new List<User> { user1 };

        var userIds = new List<Guid> { user1.Id, Guid.NewGuid(), Guid.NewGuid() };
        var command = new BulkDeleteUsersCommand(userIds);

        _userRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUsers);

        _userRepositoryMock
            .Setup(x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        _userRepositoryMock.Verify(
            x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyUserIds_ShouldNotDeleteAnything()
    {
        // Arrange
        var command = new BulkDeleteUsersCommand(Array.Empty<Guid>());

        _userRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        _userRepositoryMock.Verify(
            x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
