using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Users.Abstractions;
using GameGuild.Users.Commands;
using GameGuild.Users.Entities;
using GameGuild.Users.Models;
using GameGuild.Users.Repositories;
using Moq;
using Xunit;

namespace GameGuild.Users.UnitTests.Commands;

public class UpdateUserAccessibilityPreferencesCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserPreferencesRepository> _preferencesRepositoryMock;
    private readonly UpdateUserAccessibilityPreferencesCommandHandler _handler;

    public UpdateUserAccessibilityPreferencesCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _preferencesRepositoryMock = new Mock<IUserPreferencesRepository>();
        _handler = new UpdateUserAccessibilityPreferencesCommandHandler(_userRepositoryMock.Object, _preferencesRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingPreferences_ShouldUpdateAccessibilityPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);
        var existingPreferences = UserPreferences.Create(userId);
        
        var request = new UpdateUserAccessibilityPreferencesRequest(
            new Dictionary<string, object?> { ["fontSize"] = "large", ["highContrast"] = true }
        );
        var command = new UpdateUserAccessibilityPreferencesCommand(userId, request);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _preferencesRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPreferences);

        _preferencesRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _preferencesRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserAccessibilityPreferencesRequest(
            new Dictionary<string, object?>()
        );
        var command = new UpdateUserAccessibilityPreferencesCommand(userId, request);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() => 
            _handler.Handle(command, CancellationToken.None));
    }
}
