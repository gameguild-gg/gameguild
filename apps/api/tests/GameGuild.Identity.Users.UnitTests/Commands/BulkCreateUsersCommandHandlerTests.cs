using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Resources;
using GameGuild.Identity.Users;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class BulkCreateUsersCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IResourceQuotaService> _quotaServiceMock;
    private readonly Mock<IActorContextAccessor> _actorContextAccessorMock;
    private readonly BulkCreateUsersCommandHandler _handler;

    public BulkCreateUsersCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _quotaServiceMock = new Mock<IResourceQuotaService>();
        _actorContextAccessorMock = new Mock<IActorContextAccessor>();
        
        // Default: no tenant context (quota checks skipped)
        _actorContextAccessorMock.Setup(x => x.ActorContext).Returns(ActorContext.Anonymous);
        
        _handler = new BulkCreateUsersCommandHandler(
            _userRepositoryMock.Object,
            _quotaServiceMock.Object,
            _actorContextAccessorMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidUsers_ShouldCreateAllUsers()
    {
        // Arrange
        var userRequests = new List<CreateUserRequestItem>
        {
            new("user1@test.com", "User One", null),
            new("user2@test.com", "User Two", "+1234567890"),
            new("user3@test.com", "User Three", null)
        };
        var command = new BulkCreateUsersCommand(userRequests);

        _userRepositoryMock
            .Setup(x => x.GetByEmailsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CreatedUserIds.Should().HaveCount(3);
        result.FailedEmails.Should().BeEmpty();

        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        _userRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingEmails_ShouldFailForDuplicates()
    {
        // Arrange
        var existingUser = User.Create("existing@test.com", "Existing User", null);
        var userRequests = new List<CreateUserRequestItem>
        {
            new("existing@test.com", "Duplicate User", null),
            new("new@test.com", "New User", null)
        };
        var command = new BulkCreateUsersCommand(userRequests);

        _userRepositoryMock
            .Setup(x => x.GetByEmailsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { existingUser });

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CreatedUserIds.Should().HaveCount(1);
        result.FailedEmails.Should().Contain("existing@test.com");
        result.FailedEmails.Should().NotContain("new@test.com");

        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyList_ShouldReturnEmptyResult()
    {
        // Arrange
        var command = new BulkCreateUsersCommand(Array.Empty<CreateUserRequestItem>());

        _userRepositoryMock
            .Setup(x => x.GetByEmailsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        _userRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CreatedUserIds.Should().BeEmpty();
        result.FailedEmails.Should().BeEmpty();

        _userRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
