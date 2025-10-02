using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.UserProfiles;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Handlers;

/// <summary>
/// Unit tests for DeleteUserProfileHandler
/// </summary>
public class DeleteUserProfileHandlerTests
{
    [Fact]
    public async Task Handle_Should_Delete_UserProfile_Successfully()
    {
        // Arrange
        var mockUserProfileService = new Mock<IUserProfileService>();
        var mockLogger = new Mock<ILogger<DeleteUserProfileHandler>>();
        var mockEventPublisher = new Mock<IDomainEventPublisher>();

        var userProfileId = Guid.NewGuid();

        var command = new DeleteUserProfileCommand
        {
            UserProfileId = userProfileId,
            SoftDelete = true
        };

        var userProfile = new UserProfile
        {
            Id = userProfileId,
            DisplayName = "Test User"
        };

        mockUserProfileService.Setup(s => s.GetUserProfileByIdAsync(userProfileId))
            .ReturnsAsync(userProfile);

        mockUserProfileService.Setup(s => s.SoftDeleteUserProfileAsync(userProfileId))
            .ReturnsAsync(true);

        var handler = new DeleteUserProfileHandler(mockUserProfileService.Object, mockLogger.Object, mockEventPublisher.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }
}
