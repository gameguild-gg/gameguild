using FluentAssertions;
using GameGuild;
using GameGuild.Compliance.Audit;
using GameGuild.Identity.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Audit.Unit.Services;

public class AuditLogQueryServiceTests
{
    [Fact]
    public async Task GetPermissionLogsAsync_ShouldApplyFiltersAndMapSummary()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var matchingLog = new PermissionAuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OperationType = PermissionOperationType.Grant,
            UserId = userId,
            ResourceId = resourceId,
            ResourceType = "Asset",
            PermissionType = "Assets.Read",
            OldValue = "none",
            NewValue = "granted",
            PerformedBy = Guid.NewGuid(),
            Success = true,
            Timestamp = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc)
        };
        var logs = new List<PermissionAuditLog>
        {
            matchingLog,
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OperationType = PermissionOperationType.Revoke,
                UserId = Guid.NewGuid(),
                ResourceType = "Document",
                PermissionType = "Documents.Write",
                PerformedBy = Guid.NewGuid(),
                Success = false,
                Timestamp = matchingLog.Timestamp.AddMinutes(1)
            }
        };

        var repository = new Mock<IPermissionAuditLogRepository>();
        repository
            .Setup(mock => mock.GetByDateRangeAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                tenantId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);
        var service = new AuditLogQueryService(
            Mock.Of<IApplicationDbContext>(),
            repository.Object,
            Mock.Of<ILogger<AuditLogQueryService>>());

        var result = await service.GetPermissionLogsAsync(new PermissionAuditRequest
        {
            TenantId = tenantId,
            UserId = userId,
            OperationType = "Grant",
            PermissionType = "Assets.Read",
            ResourceType = "Asset",
            Success = true,
            Skip = 0,
            Take = 10
        });

        result.TotalCount.Should().Be(1);
        result.GrantOperations.Should().Be(1);
        result.RevokeOperations.Should().Be(0);
        result.DenyOperations.Should().Be(0);
        result.Entries.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            matchingLog.Id,
            TenantId = tenantId,
            OperationType = "Grant",
            UserId = userId,
            ResourceId = resourceId,
            ResourceType = "Asset",
            PermissionType = "Assets.Read",
            OldValue = "none",
            NewValue = "granted",
            matchingLog.PerformedBy,
            Success = true,
            matchingLog.Timestamp
        });
    }
}
