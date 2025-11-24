using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.UserProfiles;
using GameGuild.Modules.Users;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Handlers;

/// <summary>
/// Unit tests for CreateUserProfileHandler
/// </summary>
public class CreateUserProfileHandlerTests
{
    [Fact]
    public async Task Handle_Should_Create_UserProfile_Successfully()
    {
        // Arrange
        var mockUserProfileService = new Mock<IUserProfileService>();
        var mockUserService = new Mock<IUserService>();
        var mockLogger = new Mock<ILogger<CreateUserProfileHandler>>();
        var mockEventPublisher = new Mock<IDomainEventPublisher>();

        var userId = Guid.NewGuid();
        var displayName = "Test User";

        var command = new CreateUserProfileCommand
        {
            UserId = userId,
            DisplayName = displayName
        };

        var createdProfile = new UserProfile
        {
            Id = userId,
            DisplayName = displayName
        };

        mockUserProfileService.Setup(s => s.GetUserProfileByUserIdAsync(userId))
            .ReturnsAsync((UserProfile?)null);

        mockUserProfileService.Setup(s => s.CreateUserProfileAsync(It.IsAny<UserProfile>()))
            .ReturnsAsync(createdProfile);

        mockUserService.Setup(s => s.GetUserByIdAsync(userId))
            .ReturnsAsync(new User { Id = userId, Email = "test@test.com" });

        var handler = new CreateUserProfileHandler(mockUserProfileService.Object, mockUserService.Object, mockLogger.Object, mockEventPublisher.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.DisplayName.Should().Be(displayName);
    }
}
