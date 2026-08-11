using FluentAssertions;
using GameGuild;
using GameGuild.API.Database;
using GameGuild.Economy.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Testcontainers.PostgreSql;

namespace GameGuild.Economy.Payouts.UnitTests;

public sealed class PostgreSqlPayoutRequestStoreTests
{
    private static readonly DateTimeOffset Time = new(2026, 8, 11, 18, 0, 0, TimeSpan.Zero);

    [DockerFact]
    public async Task Store_PersistsReadsReplaysAndCancelsAnOwnedRequest()
    {
        await using var container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("economy_payout_request_store")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();
        await container.StartAsync();

        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(container.GetConnectionString())
                .Options);
        await context.Database.MigrateAsync();

        var payeeId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO public.economy_wallets ("Id", "OwnerId", "TenantId", "State", "CreatedAt")
            VALUES ({walletId}, {payeeId}, {Guid.NewGuid()}, 1, {Time});
            """);

        var store = new PostgreSqlPayoutRequestStore(context);
        var request = Request(payeeId, walletId);

        store.Add(request);
        store.GetForPayee(request.Id, payeeId).Should().BeEquivalentTo(request);
        store.ListForPayee(payeeId, 10).Should().ContainSingle().Which.Should().BeEquivalentTo(request);
        store.FindReplay(payeeId, request.IdempotencyKey.Value, request.RequestHash)
            .Should().BeEquivalentTo(request);
        store.FindReplay(payeeId, "missing", request.RequestHash).Should().BeNull();

        FluentActions.Invoking(() => store.FindReplay(payeeId, request.IdempotencyKey.Value, "changed"))
            .Should().Throw<PayoutRequestReplayConflictException>();
        FluentActions.Invoking(() => store.Add(request with { Id = Guid.NewGuid() }))
            .Should().Throw<PayoutRequestReplayConflictException>();
        FluentActions.Invoking(() => store.GetForPayee(Guid.NewGuid(), payeeId))
            .Should().Throw<KeyNotFoundException>();

        var cancelled = request.Cancel(Time.AddMinutes(1));
        store.Update(cancelled, request.Version).Should().BeSameAs(cancelled);
        store.GetForPayee(request.Id, payeeId).Should().BeEquivalentTo(cancelled);
        FluentActions.Invoking(() => store.Update(cancelled, request.Version))
            .Should().Throw<PayoutRequestStaleCommandException>();
    }

    [Fact]
    public void Store_RejectsInvalidConstructionAndArguments()
    {
        FluentActions.Invoking(() => new PostgreSqlPayoutRequestStore(null!))
            .Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new PostgreSqlPayoutRequestStore(new NonRelationalContext()))
            .Should().Throw<InvalidOperationException>();

        using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"payout-request-store-validation-{Guid.NewGuid():N}")
                .Options);
        var store = new PostgreSqlPayoutRequestStore(context);
        var request = Request(Guid.NewGuid(), Guid.NewGuid());

        FluentActions.Invoking(() => store.FindReplay(Guid.Empty, "request", "hash"))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => store.FindReplay(Guid.NewGuid(), "", "hash"))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => store.FindReplay(Guid.NewGuid(), "request", ""))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => store.Add(null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => store.GetForPayee(Guid.Empty, Guid.NewGuid()))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => store.GetForPayee(Guid.NewGuid(), Guid.Empty))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => store.ListForPayee(Guid.Empty, 10))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => store.ListForPayee(Guid.NewGuid(), 0))
            .Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => store.ListForPayee(Guid.NewGuid(), 101))
            .Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => store.Update(null!, 1)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => store.Update(request, 0)).Should().Throw<ArgumentOutOfRangeException>();
    }

    private static PayoutRequest Request(Guid payeeId, Guid walletId) => new(
        Guid.NewGuid(),
        new IdempotencyKey($"request-{Guid.NewGuid():N}"),
        new string('a', 64),
        payeeId,
        new WalletId(walletId),
        new CoinAmount(CurrencyCode.HardCoin, 250),
        PayoutRequestState.Submitted,
        1,
        Time,
        Time);

    private sealed class NonRelationalContext : IApplicationDbContext
    {
        public DbSet<T> Set<T>() where T : class => throw new NotSupportedException();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotSupportedException();
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotSupportedException();
        }
    }

    private sealed class DockerFactAttribute : FactAttribute
    {
        public DockerFactAttribute()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("SKIP_DOCKER_TESTS"), "1", StringComparison.Ordinal))
            {
                Skip = "Docker tests disabled by SKIP_DOCKER_TESTS=1.";
            }
        }
    }
}
