using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class BulkUnsuspendUsersCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly BulkUnsuspendUsersCommandHandler _handler;

    public BulkUnsuspendUsersCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new BulkUnsuspendUsersCommandHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidUserIds_ShouldUnsuspendAllUsers()
    {
        // Arrange
        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var users = userIds.Select(id => new User 
        { 
            Id = id, 
            Email = $"user{id}@example.com", 
            Name = $"User {id}",
            IsActive = false 
        }).ToList();
        var command = new BulkUnsuspendUsersCommand(userIds);

        _userRepositoryMock.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);
        _userRepositoryMock.Setup(x => x.UpdateRangeAsync(It.IsAny<IEnumerable<User>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UnsuspendedUsers.Should().HaveCount(2);
        result.FailedUserIds.Should().BeEmpty();
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
