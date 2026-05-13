using FluentAssertions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class UpdateUserLocalizationPreferencesCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserPreferencesRepository> _preferencesRepositoryMock;
    private readonly UpdateUserLocalizationPreferencesCommandHandler _handler;

    public UpdateUserLocalizationPreferencesCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _preferencesRepositoryMock = new Mock<IUserPreferencesRepository>();
        _handler = new UpdateUserLocalizationPreferencesCommandHandler(_userRepositoryMock.Object, _preferencesRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingPreferences_ShouldUpdateLocalizationPreferences()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);
        var existingPreferences = UserPreferences.Create(userId);
        var request = new UpdateUserLocalizationPreferencesRequest(
            JsonMap(new Dictionary<string, object?> { ["Currency"] = "BRL", ["TimeFormat"] = "24h" })
        );
        var command = new UpdateUserLocalizationPreferencesCommand(userId, request);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _preferencesRepositoryMock.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(existingPreferences);
        _preferencesRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _preferencesRepositoryMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _preferencesRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()), Times.Once);
        _preferencesRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentPreferences_ShouldCreateAndUpdateLocalizationPreferences()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);
        var request = new UpdateUserLocalizationPreferencesRequest(
            JsonMap(new Dictionary<string, object?> { ["Language"] = "es-ES" })
        );
        var command = new UpdateUserLocalizationPreferencesCommand(userId, request);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _preferencesRepositoryMock.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((UserPreferences?)null);
        _preferencesRepositoryMock.Setup(x => x.AddAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _preferencesRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _preferencesRepositoryMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _preferencesRepositoryMock.Verify(x => x.AddAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()), Times.Once);
        _preferencesRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldThrowUserNotFoundException()
    {
        var userId = Guid.NewGuid();
        var request = new UpdateUserLocalizationPreferencesRequest(JsonMap(new Dictionary<string, object?>()));
        var command = new UpdateUserLocalizationPreferencesCommand(userId, request);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UserNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
