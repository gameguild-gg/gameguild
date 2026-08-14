using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class ReplaceUserProfileCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserProfileRepository> _profileRepositoryMock;
    private readonly ReplaceUserProfileCommandHandler _handler;

    public ReplaceUserProfileCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _profileRepositoryMock = new Mock<IUserProfileRepository>();
        _handler = new ReplaceUserProfileCommandHandler(_userRepositoryMock.Object, _profileRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingProfile_ShouldReplaceProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);
        var existingProfile = UserProfile.Create(userId);

        var request = new ReplaceUserProfileRequest(
            DisplayName: "New Name",
            Bio: "New bio",
            Location: "New York",
            Website: "https://example.com",
            JobTitle: "Engineer",
            Company: "GameGuild",
            TimeZone: "UTC",
            Language: "en",
            ProfileVisibility: "public",
            ShowEmail: true,
            ShowLocation: false
        );
        var command = new ReplaceUserProfileCommand(userId, request);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _profileRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProfile);

        _profileRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.UserId.Should().Be(userId);
        result.DisplayName.Should().Be("New Name");
        result.Bio.Should().Be("New bio");
        result.Location.Should().Be("New York");
        result.Website.Should().Be("https://example.com");
        _profileRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentProfile_ShouldCreateAndReplace()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.Create("test@example.com", "Test User", null);

        var request = new ReplaceUserProfileRequest(
            DisplayName: "New Name",
            Bio: "New bio",
            Location: null,
            Website: null,
            JobTitle: null,
            Company: null,
            TimeZone: null,
            Language: null,
            ProfileVisibility: "public",
            ShowEmail: false,
            ShowLocation: false
        );
        var command = new ReplaceUserProfileCommand(userId, request);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _profileRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        _profileRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _profileRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.UserId.Should().Be(userId);
        result.DisplayName.Should().Be("New Name");
        result.Bio.Should().Be("New bio");
        _profileRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ReplaceUserProfileRequest(
            DisplayName: null,
            Bio: null,
            Location: null,
            Website: null,
            JobTitle: null,
            Company: null,
            TimeZone: null,
            Language: null,
            ProfileVisibility: "public",
            ShowEmail: false,
            ShowLocation: false
        );
        var command = new ReplaceUserProfileCommand(userId, request);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }
}
