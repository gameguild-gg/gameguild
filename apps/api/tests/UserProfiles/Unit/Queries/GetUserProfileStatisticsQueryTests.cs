using FluentAssertions;
using GameGuild.Modules.UserProfiles;
using Xunit;

namespace GameGuild.Tests.UserProfiles.Unit.Queries;

/// <summary>
/// Unit tests for GetUserProfileStatisticsQuery
/// </summary>
public class GetUserProfileStatisticsQueryTests
{
    [Fact]
    public void Query_Should_Have_Date_Range_Properties()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow;
        var tenantId = Guid.NewGuid();

        // Act
        var query = new GetUserProfileStatisticsQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            IncludeDeleted = true,
            TenantId = tenantId
        };

        // Assert
        query.FromDate.Should().Be(fromDate);
        query.ToDate.Should().Be(toDate);
        query.IncludeDeleted.Should().BeTrue();
        query.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Query_Should_Default_IncludeDeleted_To_False()
    {
        // Arrange & Act
        var query = new GetUserProfileStatisticsQuery();

        // Assert
        query.IncludeDeleted.Should().BeFalse();
    }
}
