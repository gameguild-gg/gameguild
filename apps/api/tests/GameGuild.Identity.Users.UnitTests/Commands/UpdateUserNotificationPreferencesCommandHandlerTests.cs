using FluentAssertions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class UpdateUserNotificationPreferencesCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserPreferencesRepository> _preferencesRepositoryMock;
    private readonly UpdateUserNotificationPreferencesCommandHandler _handler;

    public UpdateUserNotificationPreferencesCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _preferencesRepositoryMock = new Mock<IUserPreferencesRepository>();
        _handler = new UpdateUserNotificationPreferencesCommandHandler(_userRepositoryMock.Object, _preferencesRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingPreferences_ShouldUpdateNotificationPreferences()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);
        var existingPreferences = UserPreferences.Create(userId);
        existingPreferences.SetNotificationPreferences(new Dictionary<string, object?> { ["legacy"] = true, ["email"] = true });

        var request = new UpdateUserNotificationPreferencesRequest(
            JsonMap(new Dictionary<string, object?> { ["email"] = true, ["push"] = false })
        );
        var command = new UpdateUserNotificationPreferencesCommand(userId, request);

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
        ((System.Text.Json.JsonElement)existingPreferences.GetNotificationPreferences()["legacy"]!).GetBoolean().Should().BeTrue();
        ((System.Text.Json.JsonElement)existingPreferences.GetNotificationPreferences()["email"]!).GetBoolean().Should().BeTrue();
        ((System.Text.Json.JsonElement)existingPreferences.GetNotificationPreferences()["push"]!).GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserNotificationPreferencesRequest(
            JsonMap(new Dictionary<string, object?>())
        );
        var command = new UpdateUserNotificationPreferencesCommand(userId, request);

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
        var request = new UpdateUserNotificationPreferencesRequest(
            JsonMap(new Dictionary<string, object?> { ["email"] = false })
        );
        var command = new UpdateUserNotificationPreferencesCommand(userId, request);

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
