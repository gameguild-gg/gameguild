using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Queries;

public class GetUserPreferencesQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserPreferencesRepository> _preferencesRepositoryMock;
    private readonly GetUserPreferencesQueryHandler _handler;

    public GetUserPreferencesQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _preferencesRepositoryMock = new Mock<IUserPreferencesRepository>();
        _handler = new GetUserPreferencesQueryHandler(_userRepositoryMock.Object, _preferencesRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ShouldReturnPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);
        var query = new GetUserPreferencesQuery(userId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _preferencesRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreferences?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.Id);
        result.GeneralPreferences.Should().NotBeNull();
        result.NotificationPreferences.Should().NotBeNull();
        result.AccessibilityPreferences.Should().NotBeNull();
        result.PrivacyPreferences.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserPreferencesQuery(userId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithStoredPreferences_ShouldMergeStoredValuesIntoDto()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);
        user.Id = userId;
        user.CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        user.UpdatedAt = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc);

        var preferences = UserPreferences.Create(userId);
        preferences.CreatedAt = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        preferences.UpdatedAt = new DateTime(2024, 2, 2, 0, 0, 0, DateTimeKind.Utc);
        preferences.Version = 7;
        preferences.SetGeneralPreferences(new Dictionary<string, object?> { ["theme"] = "dark" });
        preferences.SetNotificationPreferences(new Dictionary<string, object?> { ["emailEnabled"] = false });
        preferences.SetAccessibilityPreferences(new Dictionary<string, object?> { ["fontSize"] = 18 });
        preferences.SetPrivacyPreferences(new Dictionary<string, object?> { ["profileVisibility"] = "friends" });
        preferences.SetLocalizationPreferences(new Dictionary<string, object?> { ["Language"] = "pt-BR" });

        var query = new GetUserPreferencesQuery(userId);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _preferencesRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(preferences.Id);
        result.UserId.Should().Be(userId);
        result.GeneralPreferences["theme"].GetString().Should().Be("dark");
        result.NotificationPreferences["emailEnabled"].GetBoolean().Should().BeFalse();
        result.AccessibilityPreferences["fontSize"].GetInt32().Should().Be(18);
        result.PrivacyPreferences["profileVisibility"].GetString().Should().Be("friends");
        result.LocalizationPreferences["Language"].GetString().Should().Be("pt-BR");
        result.CreatedAt.Should().Be(new DateTimeOffset(preferences.CreatedAt, TimeSpan.Zero));
        result.UpdatedAt.Should().Be(new DateTimeOffset(preferences.UpdatedAt, TimeSpan.Zero));
        result.Version.Should().BeEquivalentTo(BitConverter.GetBytes(7));
    }
}
