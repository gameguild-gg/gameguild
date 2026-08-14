using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class UpdateUserProfileCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserProfileRepository> _profileRepositoryMock;
    private readonly UpdateUserProfileCommandHandler _handler;

    public UpdateUserProfileCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _profileRepositoryMock = new Mock<IUserProfileRepository>();
        _handler = new UpdateUserProfileCommandHandler(
            _userRepositoryMock.Object,
            _profileRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", Name = "Test User" };
        var profile = new UserProfile { UserId = userId, DisplayName = "Old Name" };
        var request = new UpdateUserProfileRequest(DisplayName: "New Name", Bio: "New Bio");
        var command = new UpdateUserProfileCommand(userId, request);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepositoryMock.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _profileRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.UserId.Should().Be(userId);
        result.DisplayName.Should().Be("New Name");
        result.Bio.Should().Be("New Bio");
        profile.DisplayName.Should().Be("New Name");
        profile.Bio.Should().Be("New Bio");
        _profileRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowUserNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserProfileRequest();
        var command = new UpdateUserProfileCommand(userId, request);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenProfileDoesNotExist_ShouldCreateAndPopulateProfile()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", Name = "Test User" };
        var request = new UpdateUserProfileRequest(
            DisplayName: "Display Name",
            Bio: "Bio",
            Location: "Tokyo",
            Website: "https://example.com",
            JobTitle: "Engineer",
            Company: "GameGuild");
        var command = new UpdateUserProfileCommand(userId, request);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _profileRepositoryMock.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);
        _profileRepositoryMock.Setup(x => x.AddAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _profileRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.UserId.Should().Be(userId);
        result.DisplayName.Should().Be("Display Name");
        _profileRepositoryMock.Verify(x => x.AddAsync(
            It.Is<UserProfile>(profile =>
                profile.UserId == userId &&
                profile.DisplayName == "Display Name" &&
                profile.Bio == "Bio" &&
                profile.Location == "Tokyo" &&
                profile.Website == "https://example.com" &&
                profile.JobTitle == "Engineer" &&
                profile.Company == "GameGuild"),
            It.IsAny<CancellationToken>()), Times.Once);
        _profileRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
