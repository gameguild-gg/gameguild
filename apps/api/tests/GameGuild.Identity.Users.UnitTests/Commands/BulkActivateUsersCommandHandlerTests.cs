using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class BulkActivateUsersCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly BulkActivateUsersCommandHandler _handler;

    public BulkActivateUsersCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new BulkActivateUsersCommandHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidUserIds_ShouldActivateAllUsers()
    {
        // Arrange
        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var users = userIds.Select(id => new User 
        { 
            Id = id, 
            Email = $"user{id}@example.com", 
            Name = $"User {id}",
            IsActive = false 
        }).ToList();
        var command = new BulkActivateUsersCommand(userIds);

        _userRepositoryMock.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);
        _userRepositoryMock.Setup(x => x.UpdateRangeAsync(It.IsAny<IEnumerable<User>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ActivatedUsers.Should().HaveCount(3);
        result.FailedUserIds.Should().BeEmpty();
        users.All(u => u.IsActive).Should().BeTrue();
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Handle_WithEmptyUserIds_ShouldNotThrow()
    {
        // Arrange
        var command = new BulkActivateUsersCommand(Array.Empty<Guid>());

        _userRepositoryMock.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ActivatedUsers.Should().BeEmpty();
        result.FailedUserIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithMissingAndFailingUsers_ShouldTrackFailedUserIds()
    {
        var successUser = User.Create("success@example.com", "Success User", null);
        successUser.IsActive = false;
        var failingUser = User.Create("failing@example.com", "Failing User", null);
        failingUser.IsActive = false;
        var missingUserId = Guid.NewGuid();
        var command = new BulkActivateUsersCommand([successUser.Id, missingUserId, failingUser.Id]);

        _userRepositoryMock.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { successUser, failingUser });
        _userRepositoryMock.Setup(x => x.UpdateAsync(It.Is<User>(u => u.Id == successUser.Id), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepositoryMock.Setup(x => x.UpdateAsync(It.Is<User>(u => u.Id == failingUser.Id), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.ActivatedUsers.Should().HaveCount(1);
        result.FailedUserIds.Should().BeEquivalentTo([missingUserId, failingUser.Id]);
    }
}
