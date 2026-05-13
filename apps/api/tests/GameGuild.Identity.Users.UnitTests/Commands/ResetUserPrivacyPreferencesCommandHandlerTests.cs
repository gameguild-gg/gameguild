using FluentAssertions;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class ResetUserPrivacyPreferencesCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserPreferencesRepository> _preferencesRepositoryMock;
    private readonly ResetUserPrivacyPreferencesCommandHandler _handler;

    public ResetUserPrivacyPreferencesCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _preferencesRepositoryMock = new Mock<IUserPreferencesRepository>();
        _handler = new ResetUserPrivacyPreferencesCommandHandler(_userRepositoryMock.Object, _preferencesRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingPreferences_ShouldResetPrivacyPreferences()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);
        var existingPreferences = UserPreferences.Create(userId);
        existingPreferences.SetPrivacyPreferences(new Dictionary<string, object?> { ["profileVisible"] = true, ["shareData"] = false });
        var command = new ResetUserPrivacyPreferencesCommand(userId);

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
        existingPreferences.GetPrivacyPreferences().Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ResetUserPrivacyPreferencesCommand(userId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() => 
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithMissingPreferences_ShouldCreatePreferencesWithoutUpdating()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);
        var command = new ResetUserPrivacyPreferencesCommand(userId);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _preferencesRepositoryMock.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreferences?)null);
        _preferencesRepositoryMock.Setup(x => x.AddAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _handler.Handle(command, CancellationToken.None);

        _preferencesRepositoryMock.Verify(x => x.AddAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()), Times.Once);
        _preferencesRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
