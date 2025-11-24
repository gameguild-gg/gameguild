using FluentAssertions;
using GameGuild.UserProfiles;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Handlers;

/// <summary>
/// Unit tests for GetUserProfileStatisticsHandler
/// </summary>
public class GetUserProfileStatisticsHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Statistics()
    {
        // Arrange
        var mockUserProfileService = new Mock<IUserProfileService>();
        var mockLogger = new Mock<ILogger<GetUserProfileStatisticsHandler>>();

        var query = new GetUserProfileStatisticsQuery
        {
            FromDate = DateTime.UtcNow.AddDays(-7),
            ToDate = DateTime.UtcNow
        };

        var statistics = new UserProfileStatistics
        {
            TotalUserProfiles = 100,
            ActiveUserProfiles = 85,
            NewUserProfiles = 10
        };

        mockUserProfileService.Setup(s => s.GetStatisticsAsync(
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<Guid?>(),
            It.IsAny<bool>()))
            .ReturnsAsync(statistics);

        var handler = new GetUserProfileStatisticsHandler(mockUserProfileService.Object, mockLogger.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalUserProfiles.Should().Be(100);
        result.Value.ActiveUserProfiles.Should().Be(85);
        result.Value.NewUserProfiles.Should().Be(10);
    }
}
