using System.Text;
using FluentAssertions;
using GameGuild;
using GameGuild.Compliance.Audit;
using GameGuild.Identity.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Audit.Unit.Services;

public class AuditReportServiceTests
{
    [Fact]
    public async Task ExportAuditLogsAsync_ShouldExportUnifiedLogsAsEscapedCsv()
    {
        var request = new UnifiedSecurityAuditRequest { Skip = 25, Take = 25 };
        var entry = new UnifiedSecurityAuditEntry
        {
            Timestamp = new DateTime(2026, 5, 1, 9, 30, 0, DateTimeKind.Utc),
            SourceType = SecurityAuditSourceType.Permission,
            ActionType = "Grant,Role",
            ResourceType = "Document\"Type",
            ResourceId = "resource-123",
            UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            IpAddress = "192.0.2.10",
            Success = true,
            Description = "Granted \"editor\" access"
        };
        var queryService = new Mock<IAuditLogQueryService>();
        queryService
            .Setup(mock => mock.GetUnifiedAuditLogsAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UnifiedSecurityAuditResponse { Entries = [entry] });
        var service = new AuditReportService(
            Mock.Of<IApplicationDbContext>(),
            Mock.Of<IPermissionAuditLogRepository>(),
            queryService.Object,
            Mock.Of<ILogger<AuditReportService>>());

        var bytes = await service.ExportAuditLogsAsync(request);
        var csv = Encoding.UTF8.GetString(bytes);

        request.Skip.Should().Be(0);
        request.Take.Should().Be(10000);
        csv.Should().Contain("Timestamp,SourceType,ActionType,ResourceType,ResourceId,UserId,IpAddress,Success,Description");
        csv.Should().Contain("\"Grant,Role\"");
        csv.Should().Contain("\"Document\"\"Type\"");
        csv.Should().Contain("\"Granted \"\"editor\"\" access\"");
    }
}
