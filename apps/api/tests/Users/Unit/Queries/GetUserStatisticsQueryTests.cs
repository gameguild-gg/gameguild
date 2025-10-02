using FluentAssertions;
using GameGuild.Modules.Users;
using Xunit;

namespace GameGuild.Tests.Users.Unit.Queries;

/// <summary>
/// Unit tests for GetUserStatisticsQuery
/// </summary>
public class GetUserStatisticsQueryTests
{
    [Fact]
    public void Query_Should_Have_Date_Range_Properties()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow;

        // Act
        var query = new GetUserStatisticsQuery { FromDate = fromDate, ToDate = toDate };

        // Assert
        query.FromDate.Should().Be(fromDate);
        query.ToDate.Should().Be(toDate);
    }

    [Fact]
    public void Query_Should_Default_IncludeDeleted_To_False()
    {
        // Arrange & Act
        var query = new GetUserStatisticsQuery();

        // Assert
        query.IncludeDeleted.Should().BeFalse();
    }
}
