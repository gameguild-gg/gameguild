using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Economy.Contracts;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace GameGuild.Economy.Treasury.UnitTests;

public sealed class PostgreSqlAdminWithdrawalStoreTests
{
    private static readonly DateTimeOffset Time = new(2026, 8, 10, 18, 0, 0, TimeSpan.Zero);

    [DockerFact]
    public async Task StoreAndAudit_PersistWithdrawalLifecycle()
    {
        await using var container = new PostgreSqlBuilder().WithImage("postgres:16-alpine")
            .WithDatabase("treasury_store").WithUsername("test").WithPassword("test").Build();
        await container.StartAsync();
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(container.GetConnectionString()).Options);
        await context.Database.MigrateAsync();

        var store = new PostgreSqlAdminWithdrawalStore(context);
        var audit = new PostgreSqlAdminWithdrawalAuditTrail(context);
        var run = CreateRun();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO public.economy_wallets ("Id", "OwnerId", "TenantId", "State", "CreatedAt")
            VALUES ({run.PlatformFeeWalletId.Value}, {run.RequestedBy}, {Guid.NewGuid()}, 1, {Time});
            """);

        store.FindPeriod(run.PeriodStart).Should().BeNull();
        store.Add(run);
        store.Get(run.Id).Should().BeEquivalentTo(run);
        store.FindReplay(run.IdempotencyKey.Value, run.RequestHash).Should().BeEquivalentTo(run);
        store.FindReplay("missing", run.RequestHash).Should().BeNull();
        store.FindPeriod(run.PeriodStart).Should().BeEquivalentTo(run);
        FluentActions.Invoking(() => store.Get(Guid.NewGuid())).Should().Throw<KeyNotFoundException>();
        FluentActions.Invoking(() => store.FindReplay(run.IdempotencyKey.Value, "changed"))
            .Should().Throw<AdminWithdrawalStaleCommandException>();
        FluentActions.Invoking(() => store.Add(CreateRun(run.PeriodStart, "duplicate-period")))
            .Should().Throw<AdminWithdrawalOverlapException>();

        var approved = run with { State = AdminWithdrawalRunState.Approved, ApprovedBy = Guid.NewGuid(), Version = 2, UpdatedAt = Time.AddMinutes(1) };
        store.Update(approved, run.Version);
        var dispatching = approved with { State = AdminWithdrawalRunState.Dispatching, DispatchSnapshotHash = "dispatch", Version = 3, UpdatedAt = Time.AddMinutes(2) };
        store.Update(dispatching, approved.Version);
        var succeeded = dispatching with { State = AdminWithdrawalRunState.Succeeded, ProviderTransferId = "transfer", Version = 4, UpdatedAt = Time.AddMinutes(3) };
        store.RecordProviderEvent(" event-1 ", "event-hash", succeeded, dispatching.Version);
        store.Get(run.Id).Should().BeEquivalentTo(succeeded);
        store.FindProviderEvent("event-1", "event-hash").Should().Be(run.Id);
        store.FindProviderEvent("missing", "event-hash").Should().BeNull();
        FluentActions.Invoking(() => store.FindProviderEvent("event-1", "changed"))
            .Should().Throw<AdminWithdrawalEvidenceException>();
        FluentActions.Invoking(() => store.Update(dispatching, run.Version))
            .Should().Throw<AdminWithdrawalStaleCommandException>();

        audit.Verify(run.Id).Should().BeFalse();
        var first = audit.Append(run.Id, "requested", run.RequestedBy, "request-hash", Time);
        var second = audit.Append(run.Id, "approved", approved.ApprovedBy, "approval-hash", Time.AddMinutes(1));
        audit.Events(run.Id).Should().Equal(first, second);
        audit.Events(Guid.NewGuid()).Should().BeEmpty();
        audit.Verify(run.Id).Should().BeTrue();
    }

    private static AdminWithdrawalRun CreateRun(DateOnly? period = null, string? key = null) => new(
        Guid.NewGuid(), new IdempotencyKey(key ?? $"withdrawal-{Guid.NewGuid():N}"), "request-hash",
        period ?? new DateOnly(2026, 8, 1), Guid.NewGuid(), null, WalletId.New(),
        new CoinAmount(CurrencyCode.HardCoin, 750), "stripe:platform:cash", "destination",
        AdminWithdrawalRunState.PendingApproval, 1, 1, 1, new ReserveVersion(1), 1,
        new PolicyVersion(1), null, null, Time, Time);

    private sealed class DockerFactAttribute : FactAttribute
    {
        public DockerFactAttribute()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("SKIP_DOCKER_TESTS"), "1", StringComparison.Ordinal))
                Skip = "Docker tests disabled by SKIP_DOCKER_TESTS=1.";
        }
    }
}
