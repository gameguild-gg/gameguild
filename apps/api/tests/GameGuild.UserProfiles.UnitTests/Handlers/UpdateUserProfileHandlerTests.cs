using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.UserProfiles;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Handlers;

/// <summary>
/// Unit tests for UpdateUserProfileHandler
/// </summary>
public class UpdateUserProfileHandlerTests
{
    [Fact]
    public async Task Handle_Should_Update_UserProfile_Successfully()
    {
        // Arrange
        var mockUserProfileService = new Mock<IUserProfileService>();
        var mockLogger = new Mock<ILogger<UpdateUserProfileHandler>>();
        var mockEventPublisher = new Mock<IDomainEventPublisher>();

        var userProfileId = Guid.NewGuid();
        var oldDisplayName = "Old Name";
        var newDisplayName = "New Name";

        var command = new UpdateUserProfileCommand
        {
            UserProfileId = userProfileId,
            DisplayName = newDisplayName
        };

        var userProfile = new UserProfile
        {
            Id = userProfileId,
            DisplayName = oldDisplayName
        };

        mockUserProfileService.Setup(s => s.GetUserProfileByIdAsync(userProfileId))
            .ReturnsAsync(userProfile);

        mockUserProfileService.Setup(s => s.UpdateUserProfileAsync(userProfileId, It.IsAny<UserProfile>()))
            .ReturnsAsync(userProfile);

        var handler = new UpdateUserProfileHandler(mockUserProfileService.Object, mockLogger.Object, mockEventPublisher.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }
}
