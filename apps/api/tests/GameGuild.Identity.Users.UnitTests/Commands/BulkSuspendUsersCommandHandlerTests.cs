using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class BulkSuspendUsersCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly BulkSuspendUsersCommandHandler _handler;

    public BulkSuspendUsersCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new BulkSuspendUsersCommandHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidUserIds_ShouldSuspendAllUsers()
    {
        // Arrange
        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var users = userIds.Select(id => new User 
        { 
            Id = id, 
            Email = $"user{id}@example.com", 
            Name = $"User {id}",
            IsActive = true 
        }).ToList();
        var command = new BulkSuspendUsersCommand(userIds);

        _userRepositoryMock.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);
        _userRepositoryMock.Setup(x => x.UpdateRangeAsync(It.IsAny<IEnumerable<User>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SuspendedUsers.Should().HaveCount(2);
        result.FailedUserIds.Should().BeEmpty();
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WithMissingAndFailingUsers_ShouldTrackFailedUserIds()
    {
        var successUser = User.Create("success@example.com", "Success User", null);
        successUser.IsActive = true;
        var failingUser = User.Create("failing@example.com", "Failing User", null);
        failingUser.IsActive = true;
        var missingUserId = Guid.NewGuid();
        var command = new BulkSuspendUsersCommand([successUser.Id, missingUserId, failingUser.Id]);

        _userRepositoryMock.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { successUser, failingUser });
        _userRepositoryMock.Setup(x => x.UpdateAsync(It.Is<User>(u => u.Id == successUser.Id), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userRepositoryMock.Setup(x => x.UpdateAsync(It.Is<User>(u => u.Id == failingUser.Id), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.SuspendedUsers.Should().HaveCount(1);
        result.FailedUserIds.Should().BeEquivalentTo([missingUserId, failingUser.Id]);
    }
}
