using FluentAssertions;
using GameGuild.Compliance.Audit;
using GameGuild.Identity.Context.Actors;
using GameGuild.CQRS;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Tests.Audit.Unit.Services;

/// <summary>
/// Bulk data export detection wired into the audit-log search/export surface (issues #180/#186):
/// every <see cref="ExportAuditLogsCommand"/> flows through the bulk-export monitor with the
/// <see cref="BulkExportScope.AuditLogExport"/> scope — recorded against the tenant ledger,
/// alerted above the threshold, refused when blocking is configured, and skipped only for a
/// platform-wide export by a tenantless system administrator.
/// </summary>
public sealed class AuditLogExportBulkMonitoringTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid AdminUserId = Guid.NewGuid();

    [Fact]
    public async Task AuditLogExportIsRecordedAgainstTheTenantBulkExportLedger()
    {
        await using var context = CreateContext();
        var handler = CreateHandler(context, recordCount: 5);

        var export = await handler.Handle(
            new ExportAuditLogsCommand(AdminUserId, Guid.NewGuid(), new AuditExportRequest()),
            CancellationToken.None);

        export.TotalCount.Should().Be(5);
        var bulkExport = await context.Set<BulkExportEvent>().SingleAsync();
        bulkExport.TenantId.Should().Be(TenantId);
        bulkExport.UserId.Should().Be(AdminUserId);
        bulkExport.Scope.Should().Be(BulkExportScope.AuditLogExport);
        bulkExport.RecordCount.Should().Be(5);
        bulkExport.ThresholdExceeded.Should().BeFalse();
        (await context.Set<SecurityAlert>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AuditLogExportAboveTheThresholdRaisesASecurityAlert()
    {
        await using var context = CreateContext();
        var handler = CreateHandler(context, recordCount: 11);

        await handler.Handle(
            new ExportAuditLogsCommand(AdminUserId, Guid.NewGuid(), new AuditExportRequest()),
            CancellationToken.None);

        var bulkExport = await context.Set<BulkExportEvent>().SingleAsync();
        bulkExport.ThresholdExceeded.Should().BeTrue();
        bulkExport.AlertRaised.Should().BeTrue();
        var alert = await context.Set<SecurityAlert>().SingleAsync();
        alert.SourceActionType.Should().Be(AuditActionTypes.BulkExportThresholdExceeded);
    }

    [Fact]
    public async Task AuditLogExportIsBlockedWhenBlockingIsConfigured()
    {
        await using var context = CreateContext();
        var handler = CreateHandler(
            context,
            recordCount: 11,
            settings: new FieldLevelAuditOptions { MaxRecordsPerExport = 10, BlockExceedingExports = true });

        var command = new ExportAuditLogsCommand(AdminUserId, Guid.NewGuid(), new AuditExportRequest());
        await handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<FieldLevelAuditExportBlockedException>()
            .Where(exception => exception.RecordCount == 11 && exception.Threshold == 10);
    }

    [Fact]
    public async Task TenantlessPlatformWideExportSkipsTheBulkLedgerWithoutFailing()
    {
        await using var context = CreateContext();
        var handler = CreateHandler(context, recordCount: 5, tenantlessActor: true);

        var export = await handler.Handle(
            new ExportAuditLogsCommand(AdminUserId, Guid.NewGuid(), new AuditExportRequest()),
            CancellationToken.None);

        export.TotalCount.Should().Be(5);
        (await context.Set<BulkExportEvent>().CountAsync()).Should().Be(0,
            "a platform-wide export by a tenantless system administrator has no tenant ledger");
    }

    // ── Harness ────────────────────────────────────────────────────────────────

    private static AuditExportCommandHandler CreateHandler(
        FieldLevelBulkMonitoringContext context,
        int recordCount,
        FieldLevelAuditOptions? settings = null,
        bool tenantlessActor = false)
    {
        var auditService = new Mock<IAuditService>();
        auditService
            .Setup(service => service.GetAuditLogCountAsync(It.IsAny<AuditLogQuery>()))
            .ReturnsAsync(recordCount);
        auditService
            .Setup(service => service.StreamAuditLogsAsync(It.IsAny<AuditLogQuery>(), It.IsAny<CancellationToken>()))
            .Returns(EmptyLogs());

        var actor = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = AdminUserId.ToString(),
            TenantId = tenantlessActor ? null : TenantId,
            Roles = new HashSet<string> { tenantlessActor ? "SystemAdmin" : "Admin" },
            Permissions = new HashSet<string>(),
            IsAuthenticated = true
        };
        var actors = new Mock<IActorContextAccessor>();
        actors.Setup(accessor => accessor.ActorContext).Returns(actor);

        return new AuditExportCommandHandler(
            auditService.Object,
            Mock.Of<ISecurityAuditAggregator>(),
            new BulkExportMonitoringService(context, Options.Create(settings ?? new FieldLevelAuditOptions { MaxRecordsPerExport = 10 }), TimeProvider.System),
            actors.Object);
    }

    private static async IAsyncEnumerable<AuditLog> EmptyLogs()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static FieldLevelBulkMonitoringContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<FieldLevelBulkMonitoringContext>().UseSqlite(connection).Options;
        var context = new FieldLevelBulkMonitoringContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class FieldLevelBulkMonitoringContext(DbContextOptions<FieldLevelBulkMonitoringContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BulkExportEvent>(entity => entity.HasKey(export => export.Id));
            modelBuilder.Entity<SecurityAlert>(entity => entity.HasKey(alert => alert.Id));
        }
    }
}
