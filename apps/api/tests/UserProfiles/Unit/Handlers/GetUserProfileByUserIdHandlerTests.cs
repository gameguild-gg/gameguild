using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.UserProfiles;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Handlers;

/// <summary>
/// Unit tests for GetUserProfileByUserIdHandler
/// </summary>
public class GetUserProfileByUserIdHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_UserProfile_When_Found()
    {
        // Arrange
        var mockUserProfileService = new Mock<IUserProfileService>();
        var mockLogger = new Mock<ILogger<GetUserProfileByUserIdHandler>>();

        var userId = Guid.NewGuid();

        var query = new GetUserProfileByUserIdQuery
        {
            UserId = userId
        };

        var userProfile = new UserProfile
        {
            Id = userId,
            DisplayName = "Test User"
        };

        mockUserProfileService.Setup(s => s.GetUserProfileByUserIdAsync(userId))
            .ReturnsAsync(userProfile);

        var handler = new GetUserProfileByUserIdHandler(mockUserProfileService.Object, mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.DisplayName.Should().Be("Test User");
    }
}
