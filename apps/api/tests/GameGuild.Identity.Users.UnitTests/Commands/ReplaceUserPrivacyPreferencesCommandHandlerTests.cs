using FluentAssertions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class ReplaceUserPrivacyPreferencesCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserPreferencesRepository> _preferencesRepositoryMock;
    private readonly ReplaceUserPrivacyPreferencesCommandHandler _handler;

    public ReplaceUserPrivacyPreferencesCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _preferencesRepositoryMock = new Mock<IUserPreferencesRepository>();
        _handler = new ReplaceUserPrivacyPreferencesCommandHandler(_userRepositoryMock.Object, _preferencesRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingPreferences_ShouldReplacePrivacyPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);
        var existingPreferences = UserPreferences.Create(userId);
        
        var request = new ReplaceUserPrivacyPreferencesRequest(
            new Dictionary<string, object?> { ["profileVisible"] = false, ["shareData"] = true }
        );
        var command = new ReplaceUserPrivacyPreferencesCommand(userId, request);

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
        var request = new ReplaceUserPrivacyPreferencesRequest(
            new Dictionary<string, object?>()
        );
        var command = new ReplaceUserPrivacyPreferencesCommand(userId, request);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() => 
            _handler.Handle(command, CancellationToken.None));
    }
}
