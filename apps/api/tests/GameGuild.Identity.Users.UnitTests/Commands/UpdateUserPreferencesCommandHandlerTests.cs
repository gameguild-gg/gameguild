using FluentAssertions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class UpdateUserPreferencesCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserPreferencesRepository> _preferencesRepositoryMock;
    private readonly UpdateUserPreferencesCommandHandler _handler;

    public UpdateUserPreferencesCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _preferencesRepositoryMock = new Mock<IUserPreferencesRepository>();
        _handler = new UpdateUserPreferencesCommandHandler(
            _userRepositoryMock.Object,
            _preferencesRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdatePreferences()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", Name = "Test User" };
        var preferences = new UserPreferences { UserId = userId };
        preferences.SetGeneralPreferences(new Dictionary<string, object?> { ["theme"] = "light" });
        preferences.SetNotificationPreferences(new Dictionary<string, object?> { ["emailEnabled"] = true });
        preferences.SetAccessibilityPreferences(new Dictionary<string, object?> { ["fontSize"] = 16 });
        preferences.SetPrivacyPreferences(new Dictionary<string, object?> { ["profileVisibility"] = "public" });

        var request = new UpdateUserPreferencesRequest(
            GeneralPreferences: JsonMap(new Dictionary<string, object?> { ["theme"] = "dark", ["language"] = "en" }),
            NotificationPreferences: JsonMap(new Dictionary<string, object?> { ["pushEnabled"] = false }),
            AccessibilityPreferences: JsonMap(new Dictionary<string, object?> { ["fontSize"] = 18, ["reducedMotion"] = true }),
            PrivacyPreferences: JsonMap(new Dictionary<string, object?> { ["profileVisibility"] = "friends", ["analyticsCookies"] = false }));
        var command = new UpdateUserPreferencesCommand(userId, request);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _preferencesRepositoryMock.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);
        _preferencesRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _preferencesRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()), Times.Once);

        ((System.Text.Json.JsonElement)preferences.GetGeneralPreferences()["theme"]!).GetString().Should().Be("dark");
        ((System.Text.Json.JsonElement)preferences.GetGeneralPreferences()["language"]!).GetString().Should().Be("en");
        ((System.Text.Json.JsonElement)preferences.GetNotificationPreferences()["emailEnabled"]!).GetBoolean().Should().BeTrue();
        ((System.Text.Json.JsonElement)preferences.GetNotificationPreferences()["pushEnabled"]!).GetBoolean().Should().BeFalse();
        ((System.Text.Json.JsonElement)preferences.GetAccessibilityPreferences()["fontSize"]!).GetInt32().Should().Be(18);
        ((System.Text.Json.JsonElement)preferences.GetAccessibilityPreferences()["reducedMotion"]!).GetBoolean().Should().BeTrue();
        ((System.Text.Json.JsonElement)preferences.GetPrivacyPreferences()["profileVisibility"]!).GetString().Should().Be("friends");
        ((System.Text.Json.JsonElement)preferences.GetPrivacyPreferences()["analyticsCookies"]!).GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserPreferencesRequest();
        var command = new UpdateUserPreferencesCommand(userId, request);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenPreferencesDoNotExist_ShouldCreateAndUpdate()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", Name = "Test User" };
        var request = new UpdateUserPreferencesRequest(
            GeneralPreferences: JsonMap(new Dictionary<string, object?> { ["theme"] = "dark" }),
            NotificationPreferences: JsonMap(new Dictionary<string, object?> { ["emailEnabled"] = false }),
            AccessibilityPreferences: JsonMap(new Dictionary<string, object?> { ["fontSize"] = 20 }),
            PrivacyPreferences: JsonMap(new Dictionary<string, object?> { ["marketingEmails"] = true }));
        var command = new UpdateUserPreferencesCommand(userId, request);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _preferencesRepositoryMock.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreferences?)null);
        _preferencesRepositoryMock.Setup(x => x.AddAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _preferencesRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _preferencesRepositoryMock.Verify(x => x.AddAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()), Times.Once);
        _preferencesRepositoryMock.Verify(x => x.UpdateAsync(It.Is<UserPreferences>(prefs => prefs.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
    }
}
