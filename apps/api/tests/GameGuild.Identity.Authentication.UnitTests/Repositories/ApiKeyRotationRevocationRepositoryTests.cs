using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Repositories;

public sealed class ApiKeyRotationRevocationRepositoryTests
{
    private static readonly DateTime Now = new(2026, 11, 4, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task WorkerSweep_PersistsRevocationWithoutAuthentication_AndPreservesReplacement()
    {
        using var clock = new FixedClock();
        var options = Options();
        var tenantId = Guid.NewGuid();
        var (old, _) = ApiKey.Create(Guid.NewGuid(), tenantId, "old", ["read"]);
        old.BeginRotationGrace(Now.AddMinutes(-1));
        var (replacement, plaintext) = ApiKey.Create(old.UserId, tenantId, "replacement", ["read"]);
        replacement.ReplacesKeyId = old.Id;
        await using (var seed = new KeyContext(options))
        {
            seed.AddRange(old, replacement);
            await seed.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddScoped<IApplicationDbContext>(_ => new KeyContext(options));
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        await using var provider = services.BuildServiceProvider();
        using var worker = new ApiKeyRotationRevocationBackgroundService(
            provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<ApiKeyRotationRevocationBackgroundService>.Instance);

        (await worker.SweepOnceAsync(CancellationToken.None)).Should().Be(1);

        await using var verify = new KeyContext(options);
        var persistedOld = await verify.Set<ApiKey>().SingleAsync(key => key.Id == old.Id);
        persistedOld.IsActive.Should().BeFalse();
        persistedOld.RevokedAt.Should().Be(Now);
        persistedOld.RevocationReason.Should().Contain("overlap window closed");
        persistedOld.UsageCount.Should().Be(0);
        persistedOld.LastUsedAt.Should().BeNull("maintenance does not authenticate the key");
        var persistedNew = await verify.Set<ApiKey>().SingleAsync(key => key.Id == replacement.Id);
        persistedNew.IsActive.Should().BeTrue();
        persistedNew.RevokedAt.Should().BeNull();
        persistedNew.ValidateKey(plaintext).Should().BeTrue();
    }

    [Fact]
    public async Task Sweep_AtDeadline_FinalizesKeysAcrossOwnersAndTenants()
    {
        using var clock = new FixedClock();
        await using var context = new KeyContext(Options());
        var first = Key(Now);
        var second = Key(Now.AddSeconds(-1));
        context.AddRange(first, second);
        await context.SaveChangesAsync();

        (await new ApiKeyRepository(context).FinalizeExpiredRotationRevocationsAsync(128)).Should().Be(2);

        context.ChangeTracker.Clear();
        var keys = await context.Set<ApiKey>().ToListAsync();
        keys.Should().OnlyContain(key => !key.IsActive && key.RevokedAt == Now);
        keys.Select(key => key.UserId).Distinct().Should().HaveCount(2);
        keys.Select(key => key.TenantId).Distinct().Should().HaveCount(2);
    }

    [Fact]
    public async Task Sweep_PreservesFutureWindowsAndManualRevocations()
    {
        using var clock = new FixedClock();
        await using var context = new KeyContext(Options());
        var future = Key(Now.AddTicks(1));
        var manual = Key(Now.AddMinutes(-1));
        manual.Revoke("administrator revoked this key");
        var manualVersion = manual.Version;
        context.AddRange(future, manual);
        await context.SaveChangesAsync();
        var saves = context.SaveCount;

        (await new ApiKeyRepository(context).FinalizeExpiredRotationRevocationsAsync(128)).Should().Be(0);

        context.SaveCount.Should().Be(saves);
        context.ChangeTracker.Clear();
        (await context.Set<ApiKey>().SingleAsync(key => key.Id == future.Id)).IsActive.Should().BeTrue();
        var persistedManual = await context.Set<ApiKey>().SingleAsync(key => key.Id == manual.Id);
        persistedManual.RevocationReason.Should().Be("administrator revoked this key");
        persistedManual.Version.Should().Be(manualVersion);
        persistedManual.RevokedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Sweep_BoundsEachBatchAndProcessesOldestDeadlinesFirst_ThenIsIdempotent()
    {
        using var clock = new FixedClock();
        await using var context = new KeyContext(Options());
        var oldest = Key(Now.AddMinutes(-3));
        var middle = Key(Now.AddMinutes(-2));
        var newest = Key(Now.AddMinutes(-1));
        context.AddRange(newest, middle, oldest);
        await context.SaveChangesAsync();
        var repository = new ApiKeyRepository(context);

        (await repository.FinalizeExpiredRotationRevocationsAsync(2)).Should().Be(2);
        oldest.IsActive.Should().BeFalse();
        middle.IsActive.Should().BeFalse();
        newest.IsActive.Should().BeTrue();
        (await repository.FinalizeExpiredRotationRevocationsAsync(2)).Should().Be(1);
        var versions = new[] { oldest.Version, middle.Version, newest.Version };
        var saves = context.SaveCount;
        (await repository.FinalizeExpiredRotationRevocationsAsync(2)).Should().Be(0);
        context.SaveCount.Should().Be(saves);
        new[] { oldest.Version, middle.Version, newest.Version }.Should().Equal(versions);
    }

    [Fact]
    public async Task Sweep_EmptyStoreDoesNotSave()
    {
        await using var context = new KeyContext(Options());

        (await new ApiKeyRepository(context).FinalizeExpiredRotationRevocationsAsync(128)).Should().Be(0);

        context.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Sweep_ConcurrentVersionedRevocationIsNotOverwritten()
    {
        using var clock = new FixedClock();
        var options = Options();
        var key = Key(Now.AddMinutes(-1));
        await using (var seed = new KeyContext(options))
        {
            seed.Add(key);
            await seed.SaveChangesAsync();
        }

        var saveEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var sweepContext = new KeyContext(options)
        {
            BeforeSave = async () =>
            {
                saveEntered.TrySetResult();
                await continueSave.Task;
            }
        };
        var sweep = new ApiKeyRepository(sweepContext).FinalizeExpiredRotationRevocationsAsync(128);
        await saveEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try
        {
            await using var administrator = new KeyContext(options);
            var concurrent = await administrator.Set<ApiKey>().SingleAsync();
            concurrent.Revoke("explicit administrator decision");
            concurrent.Version++; // Competing persisted version, independent of the sweep's tracked snapshot.
            await administrator.SaveChangesAsync();
        }
        finally
        {
            continueSave.TrySetResult();
        }

        var act = async () => await sweep;
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        await using var verify = new KeyContext(options);
        var persisted = await verify.Set<ApiKey>().SingleAsync();
        persisted.RevocationReason.Should().Be("explicit administrator decision");
        persisted.Version.Should().Be(1);
        (await new ApiKeyRepository(verify).FinalizeExpiredRotationRevocationsAsync(128)).Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public async Task Sweep_RejectsUnboundedBatchSizesWithoutSaving(int batchSize)
    {
        await using var context = new KeyContext(Options());

        var act = () => new ApiKeyRepository(context).FinalizeExpiredRotationRevocationsAsync(batchSize);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        context.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Sweep_CancelledRequestDoesNotMutateOrSave()
    {
        using var clock = new FixedClock();
        await using var context = new KeyContext(Options());
        var key = Key(Now.AddMinutes(-1));
        context.Add(key);
        await context.SaveChangesAsync();
        var saves = context.SaveCount;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var act = () => new ApiKeyRepository(context).FinalizeExpiredRotationRevocationsAsync(128, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        key.IsActive.Should().BeTrue();
        key.RevokedAt.Should().BeNull();
        context.SaveCount.Should().Be(saves);
    }

    private static ApiKey Key(DateTime deadline)
    {
        var (key, _) = ApiKey.Create(Guid.NewGuid(), Guid.NewGuid(), "rotating", ["read"]);
        key.BeginRotationGrace(deadline);
        return key;
    }

    private static DbContextOptions<KeyContext> Options() => new DbContextOptionsBuilder<KeyContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;

    private sealed class KeyContext(DbContextOptions<KeyContext> options) : DbContext(options), IApplicationDbContext
    {
        public int SaveCount { get; private set; }
        public Func<Task>? BeforeSave { get; init; }
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (BeforeSave is not null) { await BeforeSave(); }
            var result = await base.SaveChangesAsync(cancellationToken);
            SaveCount++;
            return result;
        }
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfiguration(new ApiKeyConfiguration());
    }

    private sealed class FixedClock : IDisposable
    {
        public FixedClock() => SystemClock.SetProvider(new FixedProvider());
        public void Dispose() => SystemClock.Reset();
        private sealed class FixedProvider : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => new(Now);
        }
    }
}
