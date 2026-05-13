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
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);
        var existingPreferences = UserPreferences.Create(userId);
        existingPreferences.SetPrivacyPreferences(new Dictionary<string, object?> { ["legacy"] = true, ["profileVisible"] = true });

        var request = new ReplaceUserPrivacyPreferencesRequest(
            JsonMap(new Dictionary<string, object?> { ["profileVisible"] = false, ["shareData"] = true })
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

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _preferencesRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()),
            Times.Once);
        var updated = existingPreferences.GetPrivacyPreferences();
        updated.Should().HaveCount(2);
        ((System.Text.Json.JsonElement)updated["profileVisible"]!).GetBoolean().Should().BeFalse();
        ((System.Text.Json.JsonElement)updated["shareData"]!).GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ReplaceUserPrivacyPreferencesRequest(
            JsonMap(new Dictionary<string, object?>())
        );
        var command = new ReplaceUserPrivacyPreferencesCommand(userId, request);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithMissingPreferences_ShouldCreatePreferences()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);
        var request = new ReplaceUserPrivacyPreferencesRequest(
            JsonMap(new Dictionary<string, object?> { ["shareData"] = true })
        );
        var command = new ReplaceUserPrivacyPreferencesCommand(userId, request);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _preferencesRepositoryMock.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreferences?)null);
        _preferencesRepositoryMock.Setup(x => x.AddAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _preferencesRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _handler.Handle(command, CancellationToken.None);

        _preferencesRepositoryMock.Verify(x => x.AddAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()), Times.Once);
        _preferencesRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
