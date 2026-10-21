using System.Text.Json;
using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Compliance.Audit;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Tests.Audit.Unit.Services;

// =====================================================================================
// Universal entity-change capture (issue #166): the before/after snapshots staged by the
// EF change-tracking capture pass through the central PII redaction pipeline (fail-closed
// included), and the captured change history is served tenant-scoped through the
// centralized audit query surface.
// =====================================================================================

public sealed class EntityChangeAuditRedactionTests
{
    private static PiiRedactionService CreateService(IPiiRedactionRuleProvider provider)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Encryption:EncryptionKey"] = "test-key-material-for-pii-tokenization-v1!!" }).Build();
        return new PiiRedactionService(
            provider,
            new PiiHmacTokenizer(configuration),
            new EncryptionService(NullLogger<EncryptionService>.Instance, configuration),
            NullLogger<PiiRedactionService>.Instance);
    }

    private static PiiRedactionRule EmailRule() =>
        PiiRedactionRule.Create(null, "global-email", PiiRulePatternType.Preset, null, PiiPresetPattern.Email,
            PiiRuleAction.Redact, PiiRuleScope.Global, [], 100, "[EMAIL-REDACTED]", null);

    private sealed class SnapshotProvider(PiiRuleSet snapshot) : IPiiRedactionRuleProvider
    {
        public Task<PiiRuleSet> GetTenantRuleSetAsync(Guid? tenantId, CancellationToken cancellationToken) => Task.FromResult(snapshot);

        public Task<PiiRuleSet> GetGlobalRuleSetAsync(CancellationToken cancellationToken) => Task.FromResult(snapshot);

        public PiiRuleSet? TryGetGlobalSnapshot() => snapshot;

        public PiiRedactionCacheStatus GetCacheStatus() => new(true, snapshot.LoadedAt, 1, snapshot.Rules.Count, null, null);

        public void Invalidate(Guid? tenantId) { }
    }

    private sealed class ThrowingProvider : IPiiRedactionRuleProvider
    {
        public Task<PiiRuleSet> GetTenantRuleSetAsync(Guid? tenantId, CancellationToken cancellationToken) =>
            Task.FromException<PiiRuleSet>(new PiiRedactionUnavailableException(new InvalidOperationException("rule store down")));

        public Task<PiiRuleSet> GetGlobalRuleSetAsync(CancellationToken cancellationToken) =>
            Task.FromException<PiiRuleSet>(new PiiRedactionUnavailableException(new InvalidOperationException("rule store down")));

        public PiiRuleSet? TryGetGlobalSnapshot() => null;

        public PiiRedactionCacheStatus GetCacheStatus() => new(false, null, 0, 0, null, null);

        public void Invalidate(Guid? tenantId) { }
    }

    [Fact]
    public async Task ApplyToEntityChangeAuditAsync_RedactsPiiInBothSnapshots()
    {
        var provider = new SnapshotProvider(PiiRedactionRuleProvider.Compile([EmailRule()]));
        var service = CreateService(provider);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"EntityChangePii_{Guid.NewGuid()}", new InMemoryDatabaseRoot())
            .Options;
        await using var context = new TestApplicationDbContext(options);
        var record = new EntityChangeAuditRecord
        {
            EntityName = "User",
            KeyId = Guid.NewGuid().ToString(),
            ChangeKind = "Modified",
            BeforeValues = JsonSerializer.Serialize(new { Email = "before@example.com", Name = "Before name" }),
            AfterValues = JsonSerializer.Serialize(new { Email = "after@example.com", Name = "After name" }),
            TenantId = Guid.NewGuid()
        };
        context.Set<EntityChangeAuditRecord>().Add(record);

        var summary = await service.ApplyToEntityChangeAuditAsync(record, context, CancellationToken.None);

        summary.Changed.Should().BeTrue();
        summary.FailClosed.Should().BeFalse();
        record.BeforeValues.Should().Contain("[EMAIL-REDACTED]").And.NotContain("before@example.com");
        record.AfterValues.Should().Contain("[EMAIL-REDACTED]").And.NotContain("after@example.com");
        // Unmatched values survive untouched so investigations keep the non-PII delta.
        record.BeforeValues.Should().Contain("Before name");
        record.AfterValues.Should().Contain("After name");

        await context.SaveChangesAsync();
        var operation = await context.Set<PiiRedactionOperation>().SingleAsync();
        operation.Source.Should().Be(PiiRedactionSource.EntityChangeCapture);
        operation.FailClosed.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyToEntityChangeAuditAsync_FailsClosed_WhenRulesAreUnavailable()
    {
        var service = CreateService(new ThrowingProvider());
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"EntityChangePiiFailClosed_{Guid.NewGuid()}", new InMemoryDatabaseRoot())
            .Options;
        await using var context = new TestApplicationDbContext(options);
        var record = new EntityChangeAuditRecord
        {
            EntityName = "User",
            KeyId = Guid.NewGuid().ToString(),
            ChangeKind = "Modified",
            BeforeValues = JsonSerializer.Serialize(new { Email = "before@example.com" }),
            AfterValues = JsonSerializer.Serialize(new { Email = "after@example.com" }),
            TenantId = Guid.NewGuid()
        };
        context.Set<EntityChangeAuditRecord>().Add(record);

        var summary = await service.ApplyToEntityChangeAuditAsync(record, context, CancellationToken.None);

        summary.FailClosed.Should().BeTrue();
        record.BeforeValues.Should().Be(PiiRedactionFailClosed.Marker);
        record.AfterValues.Should().Be(PiiRedactionFailClosed.Marker);

        await context.SaveChangesAsync();
        var operation = await context.Set<PiiRedactionOperation>().SingleAsync();
        operation.Source.Should().Be(PiiRedactionSource.EntityChangeCapture);
        operation.FailClosed.Should().BeTrue();
    }

    [Fact]
    public async Task SaveChanges_InterceptorAppliesPipelineToAutomaticallyCapturedSnapshots()
    {
        var provider = new SnapshotProvider(PiiRedactionRuleProvider.Compile([EmailRule()]));
        var service = CreateService(provider);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"EntityChangePiiInterceptor_{Guid.NewGuid()}", new InMemoryDatabaseRoot())
            .AddInterceptors(new AuditLogPiiRedactionInterceptor(service))
            .Options;
        await using var context = new TestApplicationDbContext(options);
        var user = new User { Email = "captured@example.com", Name = "Intercepted user" };
        context.Add(user);
        await context.SaveChangesAsync();

        user.Email = "rotated@example.com";
        await context.SaveChangesAsync();

        var records = await context.Set<EntityChangeAuditRecord>().IgnoreQueryFilters()
            .Where(record => record.ChangeKind == "Modified").ToListAsync();
        records.Should().ContainSingle();
        var modified = records[0];
        modified.BeforeValues.Should().NotContain("captured@example.com");
        modified.AfterValues.Should().NotContain("rotated@example.com");
        modified.AfterValues.Should().Contain("[EMAIL-REDACTED]");

        // The pipeline application itself is audited through the redaction-operation trail.
        var operations = await context.Set<PiiRedactionOperation>().Where(op => op.Source == PiiRedactionSource.EntityChangeCapture).ToListAsync();
        operations.Should().NotBeEmpty();
    }
}

public sealed class EntityChangeAuditQueryServiceTests
{
    private static EntityChangeAuditRecord Record(Guid? tenantId, string entityName, DateTimeOffset occurredAt) => new()
    {
        EntityName = entityName,
        KeyId = Guid.NewGuid().ToString(),
        ChangeKind = "Added",
        AfterValues = JsonSerializer.Serialize(new { Title = "Snapshot" }),
        TenantId = tenantId,
        OccurredAtUtc = occurredAt
    };

    private static async Task<TestApplicationDbContext> SeededContextAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"EntityChangeQuery_{Guid.NewGuid()}", new InMemoryDatabaseRoot())
            .Options;
        var context = new TestApplicationDbContext(options);
        var tenant = Guid.NewGuid();
        context.Set<EntityChangeAuditRecord>().AddRange(
            Record(tenant, "Project", DateTimeOffset.UtcNow.AddMinutes(-1)),
            Record(tenant, "User", DateTimeOffset.UtcNow.AddMinutes(-2)),
            Record(Guid.NewGuid(), "Project", DateTimeOffset.UtcNow),
            Record(null, "PlatformSetting", DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task Store_ReturnsOnlyTheRequestedTenantRecords_NewestFirst()
    {
        await using var context = await SeededContextAsync();
        var tenantRecords = await context.Set<EntityChangeAuditRecord>().IgnoreQueryFilters()
            .Where(record => record.TenantId != null && record.EntityName == "Project")
            .ToListAsync();
        var tenantId = tenantRecords[0].TenantId;

        var store = new EntityChangeAuditStore(context);
        var result = await store.QueryAsync(new EntityChangeAuditQuery(TenantId: tenantId, Take: 10), CancellationToken.None);

        result.Should().OnlyContain(entry => entry.TenantId == tenantId);
        result.Select(entry => entry.EntityName).Should().ContainSingle("Project");
        result[0].OccurredAtUtc.Should().Be(result.Max(entry => entry.OccurredAtUtc));

        var count = await store.CountAsync(new EntityChangeAuditQuery(TenantId: tenantId), CancellationToken.None);
        count.Should().Be(1);
    }

    [Fact]
    public async Task QueryService_RequiresTenantAdministrator()
    {
        await using var context = await SeededContextAsync();
        var actors = new Mock<IActorContextAccessor>();
        actors.SetupGet(accessor => accessor.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            IsAuthenticated = false,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>()
        });
        var service = new EntityChangeAuditQueryService(actors.Object, new EntityChangeAuditStore(context),
            NullLogger<EntityChangeAuditQueryService>.Instance);

        var act = () => service.ListForCurrentTenantAsync(new EntityChangeAuditListRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task QueryService_ServesCurrentTenantHistory_WithTotalCount()
    {
        await using var context = await SeededContextAsync();
        var tenantId = await context.Set<EntityChangeAuditRecord>().IgnoreQueryFilters()
            .Where(record => record.TenantId != null && record.EntityName == "Project")
            .Select(record => record.TenantId!.Value)
            .FirstAsync();
        var actors = new Mock<IActorContextAccessor>();
        actors.SetupGet(accessor => accessor.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = tenantId,
            IsAuthenticated = true,
            Roles = new HashSet<string> { "TenantAdmin" },
            Permissions = new HashSet<string>()
        });
        var service = new EntityChangeAuditQueryService(actors.Object, new EntityChangeAuditStore(context),
            NullLogger<EntityChangeAuditQueryService>.Instance);

        var response = await service.ListForCurrentTenantAsync(
            new EntityChangeAuditListRequest { EntityName = "Project" }, CancellationToken.None);

        response.TotalCount.Should().Be(1);
        response.Changes.Should().ContainSingle().Which.EntityName.Should().Be("Project");
    }
}
