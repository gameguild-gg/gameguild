using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.UserProfiles;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Handlers;

/// <summary>
/// Unit tests for RestoreUserProfileHandler
/// </summary>
public class RestoreUserProfileHandlerTests
{
    [Fact]
    public async Task Handle_Should_Restore_UserProfile_Successfully()
    {
        // Arrange
        var mockUserProfileService = new Mock<IUserProfileService>();
        var mockLogger = new Mock<ILogger<RestoreUserProfileHandler>>();
        var mockEventPublisher = new Mock<IDomainEventPublisher>();

        var userProfileId = Guid.NewGuid();

        var command = new RestoreUserProfileCommand
        {
            UserProfileId = userProfileId
        };

        var userProfile = new UserProfile
        {
            Id = userProfileId,
            DisplayName = "Test User",
            DeletedAt = DateTime.UtcNow
        };

        mockUserProfileService.Setup(s => s.GetUserProfileByIdAsync(userProfileId))
            .ReturnsAsync(userProfile);

        mockUserProfileService.Setup(s => s.RestoreUserProfileAsync(userProfileId))
            .ReturnsAsync(true);

        var handler = new RestoreUserProfileHandler(mockUserProfileService.Object, mockLogger.Object, mockEventPublisher.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }
}
