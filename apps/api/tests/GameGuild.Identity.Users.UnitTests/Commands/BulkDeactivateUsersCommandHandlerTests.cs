using FluentAssertions;
using GameGuild.Identity.Users;
using GameGuild.Identity.Users;
using GameGuild.Identity.Users;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class BulkDeactivateUsersCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly BulkDeactivateUsersCommandHandler _handler;

    public BulkDeactivateUsersCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new BulkDeactivateUsersCommandHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidUserIds_ShouldDeactivateAllUsers()
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
        var command = new BulkDeactivateUsersCommand(userIds);

        _userRepositoryMock.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);
        _userRepositoryMock.Setup(x => x.UpdateRangeAsync(It.IsAny<IEnumerable<User>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.DeactivatedUsers.Should().HaveCount(2);
        result.FailedUserIds.Should().BeEmpty();
        users.All(u => !u.IsActive).Should().BeTrue();
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
