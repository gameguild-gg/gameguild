using FluentAssertions;
using GameGuild.Identity.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

public sealed class PermissionAnalyticsServiceTrendTests
{
    [Fact]
    public async Task GetPermissionTrendsAsync_ReturnsCumulativeActivePermissions()
    {
        var from = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddDays(3);
        var logs = new List<PermissionAuditLog>
        {
            new() { OperationType = PermissionOperationType.Grant, Timestamp = from.AddHours(1) },
            new() { OperationType = PermissionOperationType.Grant, Timestamp = from.AddHours(2) },
            new() { OperationType = PermissionOperationType.Revoke, Timestamp = from.AddDays(1).AddHours(1) },
            new() { OperationType = PermissionOperationType.Grant, Timestamp = from.AddDays(2).AddHours(1) },
            new() { OperationType = PermissionOperationType.Revoke, Timestamp = from.AddDays(2).AddHours(2) }
        };

        var repository = new Mock<IPermissionAuditLogRepository>();
        repository
            .Setup(r => r.GetByDateRangeAsync(from, to, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        var service = new PermissionAnalyticsService(
            repository.Object,
            NullLogger<PermissionAnalyticsService>.Instance);

        var trends = await service.GetPermissionTrendsAsync(null, from, to, CancellationToken.None);

        trends.Select(t => t.ActivePermissions).Should().Equal(2, 1, 1);
        trends.Select(t => t.Grants).Should().Equal(2, 0, 1);
        trends.Select(t => t.Revokes).Should().Equal(0, 1, 1);
    }
}
