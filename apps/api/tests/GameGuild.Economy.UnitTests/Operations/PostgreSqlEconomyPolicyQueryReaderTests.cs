using FluentAssertions;
using System.Text.Json;
using GameGuild.Economy.Operations;
using GameGuild.Economy.Persistence;
using GameGuild.Economy.Risk;
using GameGuild.TestSupport.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameGuild.Economy.UnitTests.Operations;

public sealed class PostgreSqlEconomyPolicyQueryReaderTests
{
    private static readonly Guid TenantId = Guid.Parse("ac000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ListsOnlyGlobalAndActorTenantPoliciesWithStableCursorAndAudit()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("policy_queries");
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.EnsureCreatedAsync();
        var global = Policy(null, 1, Now.AddMinutes(-3));
        var tenant = Policy(TenantId, 2, Now.AddMinutes(-2));
        context.Set<EconomyCapabilityPolicyRow>().AddRange(
            global,
            tenant,
            Policy(TenantId, 3, Now.AddMinutes(-1)),
            Policy(Guid.NewGuid(), 4, Now));
        context.Set<EconomyCapabilityPolicyApprovalRow>().Add(new EconomyCapabilityPolicyApprovalRow
        {
            Id = Guid.NewGuid(),
            PolicyId = tenant.Id,
            ActorId = tenant.ApprovedBy!.Value,
            ReauthenticationHash = "approval-evidence",
            ApprovedAt = tenant.ApprovedAt!.Value
        });
        await context.SaveChangesAsync();
        var reader = new PostgreSqlEconomyPolicyQueryReader(context);

        var first = await reader.ListAsync(TenantId, null, 2, null, Now, default);
        var second = await reader.ListAsync(TenantId, null, 2, first.NextCursor, Now, default);
        var detail = await reader.FindAsync(TenantId, tenant.Id, Now, default);
        var audit = await reader.ReadAuditAsync(TenantId, tenant.Id, default);

        first.Items.Select(item => item.Version).Should().Equal(3, 2);
        first.NextCursor.Should().NotBeNull();
        second.Items.Select(item => item.Version).Should().Equal(1);
        second.NextCursor.Should().BeNull();
        detail.Should().NotBeNull();
        using var payload = JsonDocument.Parse(detail!.CanonicalPayload);
        payload.RootElement.GetProperty("provider").GetString().Should().Be("stripe");
        audit.Select(item => item.Kind).Should().Equal("Proposed", "Approved");
        audit.Last().EvidenceHash.Should().Be("approval-evidence");
    }

    [Fact]
    public async Task RejectsForeignPolicyAndInvalidCursor()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("policy_queries_scope");
        await using var context = CreateContext(database.ConnectionString);
        await context.Database.EnsureCreatedAsync();
        var foreign = Policy(Guid.NewGuid(), 1, Now);
        context.Set<EconomyCapabilityPolicyRow>().Add(foreign);
        await context.SaveChangesAsync();
        var reader = new PostgreSqlEconomyPolicyQueryReader(context);

        (await reader.FindAsync(TenantId, foreign.Id, Now, default)).Should().BeNull();
        await FluentActions.Awaiting(() => reader.ListAsync(
                TenantId, null, 10, "invalid", Now, default).AsTask())
            .Should().ThrowAsync<ArgumentException>();
    }

    private static EconomyCapabilityPolicyRow Policy(Guid? tenantId, long version, DateTimeOffset proposedAt)
    {
        var proposedBy = Guid.NewGuid();
        return new EconomyCapabilityPolicyRow
        {
            Id = Guid.NewGuid(),
            ScopeKey = tenantId is null ? $"global:5:BR:{version}" : $"{tenantId:N}:5:BR:{version}",
            TenantId = tenantId,
            Capability = EconomyValueMovementCapability.PayoutExecution,
            JurisdictionCode = "BR",
            Version = version,
            CanonicalPayload = "{\"provider\":\"stripe\"}",
            PayloadHash = $"policy-{version}",
            KeyId = "kms-key",
            Signature = "signature",
            RequestHash = $"request-{version}",
            ProposedBy = proposedBy,
            ApprovedBy = proposedBy == Guid.Empty ? null : Guid.NewGuid(),
            ProposedAt = proposedAt,
            ApprovedAt = proposedAt.AddMinutes(1),
            EffectiveAt = proposedAt.AddMinutes(1),
            ExpiresAt = Now.AddDays(1),
            ProviderReady = true,
            IsActive = true
        };
    }

    private static QueryDbContext CreateContext(string connectionString) => new(
        new DbContextOptionsBuilder<QueryDbContext>()
            .UseNpgsql(connectionString)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options);

    private sealed class QueryDbContext(DbContextOptions<QueryDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            new EconomyModelConfiguration().Configure(modelBuilder);

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            Database.BeginTransactionAsync(cancellationToken);
    }
}
