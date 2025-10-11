using FluentAssertions;
using GameGuild.Modules.UserProfiles;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Handlers;

/// <summary>
/// Unit tests for GetAllUserProfilesHandler
/// </summary>
public class GetAllUserProfilesHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_UserProfiles()
    {
        // Arrange
        var mockUserProfileService = new Mock<IUserProfileService>();
        var mockLogger = new Mock<ILogger<GetAllUserProfilesHandler>>();

        var query = new GetAllUserProfilesQuery
        {
            Skip = 0,
            Take = 10
        };

        var userProfiles = new List<UserProfile>
        {
            new() { Id = Guid.NewGuid(), DisplayName = "User 1" },
            new() { Id = Guid.NewGuid(), DisplayName = "User 2" }
        };

        mockUserProfileService.Setup(s => s.GetAllUserProfilesAsync())
            .ReturnsAsync(userProfiles);

        var handler = new GetAllUserProfilesHandler(mockUserProfileService.Object, mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().HaveCount(2);
    }
}
