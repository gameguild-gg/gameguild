using FluentAssertions;
using GameGuild.Identity.Users;
using GameGuild.Identity.Users;
using GameGuild.Identity.Users;
using GameGuild.Identity.Users;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Queries;

public class GetUserPreferencesQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly GetUserPreferencesQueryHandler _handler;

    public GetUserPreferencesQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new GetUserPreferencesQueryHandler(_userRepositoryMock.Object);
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
}
