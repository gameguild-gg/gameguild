using FluentAssertions;
using GameGuild.Modules.Users;
using Moq;
using Xunit;

namespace GameGuild.Tests.Users.Unit.Handlers;

/// <summary>
/// Unit tests for GetUserStatisticsHandler
/// </summary>
public class GetUserStatisticsHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Statistics()
    {
        // Arrange
        var mockUserRepository = new Mock<IUserRepository>();
        var expectedStats = new UserStatistics
        {
            TotalUsers = 100,
            ActiveUsers = 85,
            InactiveUsers = 10,
            DeletedUsers = 5,
            UsersCreatedToday = 3,
            UsersCreatedThisWeek = 12,
            UsersCreatedThisMonth = 25
        };

        mockUserRepository.Setup(r => r.GetUserStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStats);

        var handler = new GetUserStatisticsHandler(mockUserRepository.Object);
        var query = new GetUserStatisticsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalUsers.Should().Be(100);
        result.ActiveUsers.Should().Be(85);
        result.InactiveUsers.Should().Be(10);
        result.DeletedUsers.Should().Be(5);
        result.UsersCreatedToday.Should().Be(3);
        result.UsersCreatedThisWeek.Should().Be(12);
        result.UsersCreatedThisMonth.Should().Be(25);
    }
}
