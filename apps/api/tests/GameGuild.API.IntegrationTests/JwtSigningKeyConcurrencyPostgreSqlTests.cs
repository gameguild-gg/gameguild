using System.Data.Common;
using System.Security.Cryptography;
using GameGuild;
using GameGuild.Identity.Authentication;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

using Xunit;

namespace GameGuild.API.IntegrationTests;

public sealed class JwtSigningKeyConcurrencyPostgreSqlTests(JwtKeyConcurrencyFixture fixture) : IClassFixture<JwtKeyConcurrencyFixture>, IAsyncLifetime
{
    private readonly CancellationTokenSource _deadline = new(TimeSpan.FromMinutes(2));
    private DbContextOptions<JwtConcurrencyContext> Options => new DbContextOptionsBuilder<JwtConcurrencyContext>().UseNpgsql(fixture.ConnectionString).Options;
    public async Task InitializeAsync()
    {
        await using var db = new JwtConcurrencyContext(Options);
        await db.Set<JwtSigningKey>().ExecuteDeleteAsync(_deadline.Token);
    }
    public Task DisposeAsync() { _deadline.Dispose(); return Task.CompletedTask; }
    private static IKeyRotationService Service(JwtConcurrencyContext db, JwtSigningKeyCache? cache = null) =>
        new KeyRotationService(db, NullLogger<KeyRotationService>.Instance, cache);
    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static void Check(bool condition, string explanation) => Assert.True(condition, explanation);
    private async Task<List<JwtSigningKey>> Persisted()
    {
        await using var db = new JwtConcurrencyContext(Options);
        return await db.Set<JwtSigningKey>().AsNoTracking().OrderBy(key => key.KeyVersion).ToListAsync(_deadline.Token);
    }

    [Fact]
    public async Task SequentialInitializeIsIdempotentControl()
    {
        await using var db = new JwtConcurrencyContext(Options);
        await Service(db).InitializeAsync(_deadline.Token);
        await Service(db).InitializeAsync(_deadline.Token);
        var keys = await Persisted();
        Check(keys.Count == 1 && keys[0].IsActive && keys[0].KeyVersion == 1, "Expected one initialized key.");
    }

    [Fact]
    public async Task ConcurrentInitializeCommitsOneActiveGeneration()
    {
        var firstSave = Signal(); var secondApproach = Signal(); var release = Signal();
        await using var first = new JwtConcurrencyContext(Options, async () => { firstSave.TrySetResult(); await release.Task.WaitAsync(_deadline.Token); });
        var secondOptions = new DbContextOptionsBuilder<JwtConcurrencyContext>().UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(new JwtConcurrencyFenceInterceptor(secondApproach)).Options;
        await using var second = new JwtConcurrencyContext(secondOptions, () => { secondApproach.TrySetResult(); return Task.CompletedTask; });
        var firstTask = Service(first).InitializeAsync(_deadline.Token);
        await firstSave.Task.WaitAsync(_deadline.Token);
        var secondTask = Service(second).InitializeAsync(_deadline.Token);
        try { await secondApproach.Task.WaitAsync(_deadline.Token); }
        finally { release.TrySetResult(); }
        await Task.WhenAll(firstTask, secondTask);
        var keys = await Persisted();
        Check(keys.Count == 1 && keys.Count(key => key.IsActive) == 1 && keys[0].KeyVersion == 1,
            "Concurrent initialization persisted competing active generations.");
    }

    [Fact]
    public async Task RotationSavesActivationAndDeactivationOnce()
    {
        await using var seed = new JwtConcurrencyContext(Options);
        await Service(seed).InitializeAsync(_deadline.Token);
        await using var db = new JwtConcurrencyContext(Options);
        var next = await Service(db).RotateKeyAsync(cancellationToken: _deadline.Token);
        var keys = await Persisted();
        Check(db.SaveCalls == 1 && keys.Count == 2 && keys.Count(key => key.IsActive) == 1 && keys.Single(key => key.IsActive).Id == next.Id,
            "Rotation did not persist the key transition in one save batch.");
    }

    [Fact]
    public async Task ConcurrentRotationsAreSerializedWithDistinctVersions()
    {
        await using (var seed = new JwtConcurrencyContext(Options)) { await Service(seed).InitializeAsync(_deadline.Token); }
        var firstSave = Signal(); var secondApproach = Signal(); var release = Signal();
        await using var first = new JwtConcurrencyContext(Options, async () => { firstSave.TrySetResult(); await release.Task.WaitAsync(_deadline.Token); });
        var secondOptions = new DbContextOptionsBuilder<JwtConcurrencyContext>().UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(new JwtConcurrencyFenceInterceptor(secondApproach)).Options;
        await using var second = new JwtConcurrencyContext(secondOptions, () => { secondApproach.TrySetResult(); return Task.CompletedTask; });
        var firstTask = Service(first).RotateKeyAsync(cancellationToken: _deadline.Token);
        await firstSave.Task.WaitAsync(_deadline.Token);
        var secondTask = Service(second).RotateKeyAsync(cancellationToken: _deadline.Token);
        try { await secondApproach.Task.WaitAsync(_deadline.Token); }
        finally { release.TrySetResult(); }
        var generated = await Task.WhenAll(firstTask, secondTask);
        var keys = await Persisted();
        Check(keys.Select(key => key.KeyVersion).SequenceEqual(new[] { 1, 2, 3 }) && keys.Count(key => key.IsActive) == 1,
            "Concurrent rotations did not serialize generations 1, 2, 3 with one active key.");
        Check(generated.Select(key => key.Id).Distinct().Count() == 2, "Two requests did not return distinct completed rotations.");
    }

    [Fact]
    public async Task FailedWriteLeavesOldKeyAndCacheUnchanged()
    {
        var cache = new JwtSigningKeyCache();
        await using (var seed = new JwtConcurrencyContext(Options)) { await Service(seed, cache).InitializeAsync(_deadline.Token); }
        var before = (await Persisted()).Single();
        await using var failing = new JwtConcurrencyContext(Options, () => throw new JwtConcurrencyWriteFailureException());
        await Assert.ThrowsAsync<JwtConcurrencyWriteFailureException>(
            () => Service(failing, cache).RotateKeyAsync(cancellationToken: _deadline.Token));
        var after = await Persisted();
        var fallback = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64)) { KeyId = "synthetic-fallback" };
        Check(after.Count == 1 && after[0].Id == before.Id && after[0].IsActive, "Failed rotation changed the committed key set.");
        Check(cache.GetValidationKeys(fallback).Where(key => key.KeyId != fallback.KeyId).Select(key => key.KeyId).SequenceEqual(new[] { before.KeyId }),
            "Failed rotation published a new validation key.");
    }

    [Fact]
    public async Task CallerTransactionIsNotCommittedAndCacheIgnoresRollback()
    {
        var cache = new JwtSigningKeyCache();
        await using (var seed = new JwtConcurrencyContext(Options)) { await Service(seed, cache).InitializeAsync(_deadline.Token); }
        var before = (await Persisted()).Single();
        await using var db = new JwtConcurrencyContext(Options);
        await using var transaction = await db.Database.BeginTransactionAsync(_deadline.Token);
        var next = await Service(db, cache).RotateKeyAsync(cancellationToken: _deadline.Token);
        Check(ReferenceEquals(transaction, db.Database.CurrentTransaction), "The service took ownership of the caller transaction.");
        var external = await Persisted();
        Check(external.Count == 1 && external[0].Id == before.Id && external[0].IsActive, "The service committed the caller transaction.");
        var fallback = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64)) { KeyId = "synthetic-fallback" };
        Check(!cache.GetValidationKeys(fallback).Any(key => key.KeyId == next.KeyId), "The cache published the caller's uncommitted key.");
        await transaction.RollbackAsync(_deadline.Token);
        var after = await Persisted();
        Check(after.Count == 1 && after[0].IsActive, "Caller rollback did not restore the original active key.");
    }

    [Fact]
    public async Task CancellationWhileWaitingDoesNotMutateOrLeakFence()
    {
        await using var owner = new JwtConcurrencyContext(Options);
        await using var transaction = await owner.Database.BeginTransactionAsync(_deadline.Token);
        await Service(owner).InitializeAsync(_deadline.Token);
        var attempted = Signal();
        var waitingOptions = new DbContextOptionsBuilder<JwtConcurrencyContext>().UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(new JwtConcurrencyFenceInterceptor(attempted)).Options;
        await using var waiting = new JwtConcurrencyContext(waitingOptions);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_deadline.Token);
        var wait = Service(waiting).InitializeAsync(cancellation.Token);
        await Task.WhenAny(wait, attempted.Task).WaitAsync(_deadline.Token);
        Check(!wait.IsCompleted, "A competing initializer did not wait for the uncommitted owner.");
        await attempted.Task.WaitAsync(_deadline.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
        Check(cancellation.IsCancellationRequested, "Expected the caller to cancel the waiter.");
        await transaction.CommitAsync(_deadline.Token);
        await using var third = new JwtConcurrencyContext(Options);
        await Service(third).InitializeAsync(_deadline.Token);
        var keys = await Persisted();
        Check(keys.Count == 1 && keys[0].IsActive, "Cancelled waiter changed keys or retained the fence.");
    }

    [Fact]
    public async Task ExpiredGenerationDoesNotResetVersionOrResurrectExpiry()
    {
        var now = DateTime.UtcNow;
        var old = JwtSigningKey.CreateNew(7, now.AddDays(-3), TimeSpan.FromDays(1)); old.Activate();
        await using (var seed = new JwtConcurrencyContext(Options)) { seed.Add(old); await seed.SaveChangesAsync(_deadline.Token); }
        var persistedExpiry = (await Persisted()).Single().ExpiresAt;
        await using var db = new JwtConcurrencyContext(Options);
        await Service(db).InitializeAsync(_deadline.Token);
        var keys = await Persisted();
        Check(keys.Count == 2 && keys.Last().KeyVersion == 8 && keys.Count(key => key.IsActive) == 1,
            "An expired active generation reset the version or remained active.");
        Check(keys[0].ExpiresAt == persistedExpiry, "Rotation resurrected an expired validation key.");
    }

    [Fact]
    public async Task LegacyCompetingLiveKeysAreRetiredWithoutLosingOverlap()
    {
        var now = DateTime.UtcNow;
        await using (var seed = new JwtConcurrencyContext(Options))
        {
            foreach (var version in new[] { 1, 2 }) { var key = JwtSigningKey.CreateNew(version, now.AddMinutes(-1), TimeSpan.FromMinutes(2)); key.Activate(); seed.Add(key); }
            await seed.SaveChangesAsync(_deadline.Token);
        }
        await using var db = new JwtConcurrencyContext(Options);
        await Service(db).RotateKeyAsync(cancellationToken: _deadline.Token);
        var keys = await Persisted();
        Check(keys.Count == 3 && keys.Count(key => key.IsActive) == 1 && keys.Last().KeyVersion == 3,
            "Rotation retained multiple active predecessors.");
        Check(keys.Take(2).All(key => key.ExpiresAt >= now.AddMinutes(60)), "A legacy live predecessor lost its token-overlap window.");
    }

    [Fact]
    public async Task TransientFailureAfterSaveRollsBackAndRetriesCleanly()
    {
        await using (var seed = new JwtConcurrencyContext(Options)) { await Service(seed).InitializeAsync(_deadline.Token); }
        var retryOptions = new DbContextOptionsBuilder<JwtConcurrencyContext>().UseNpgsql(fixture.ConnectionString,
            postgres => postgres.EnableRetryOnFailure(1, TimeSpan.Zero, null)).Options;
        await using var db = new JwtConcurrencyContext(retryOptions, failAfterSaveOnce: true);
        await Service(db).RotateKeyAsync(cancellationToken: _deadline.Token);
        var keys = await Persisted();
        Check(keys.Select(key => key.KeyVersion).SequenceEqual(new[] { 1, 2 }) && keys.Count(key => key.IsActive) == 1,
            "A retried failed save duplicated or lost a generation.");
        Check(db.SaveCalls == 2, "The injected transient failure was not retried once.");
    }

    [Fact]
    public async Task AmbiguousCommitAcknowledgmentDoesNotRotateTwice()
    {
        await using (var seed = new JwtConcurrencyContext(Options)) { await Service(seed).InitializeAsync(_deadline.Token); }
        var failure = new JwtConcurrencyCommitFailure();
        var retryOptions = new DbContextOptionsBuilder<JwtConcurrencyContext>().UseNpgsql(fixture.ConnectionString,
            postgres => postgres.EnableRetryOnFailure(1, TimeSpan.Zero, null)).AddInterceptors(failure).Options;
        await using var db = new JwtConcurrencyContext(retryOptions);
        var next = await Service(db).RotateKeyAsync(cancellationToken: _deadline.Token);
        var keys = await Persisted();
        Check(failure.Injections == 1 && keys.Select(key => key.KeyVersion).SequenceEqual(new[] { 1, 2 }) && keys.Count(key => key.IsActive) == 1,
            "Commit acknowledgment loss duplicated or lost the rotation.");
        Check(keys.Single(key => key.IsActive).Id == next.Id, "Retry returned a different rotation from the committed operation.");
    }
}

sealed class JwtConcurrencyWriteFailureException : Exception;
sealed class JwtConcurrencyCommitFailure : DbTransactionInterceptor
{
    public int Injections { get; private set; }
    public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (Injections == 0)
        {
            Injections++;
            throw new Npgsql.NpgsqlException("Synthetic lost commit acknowledgment", new IOException());
        }
        return Task.CompletedTask;
    }
}
sealed class JwtConcurrencyFenceInterceptor(TaskCompletionSource attempted) : DbCommandInterceptor
{
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("pg_advisory_xact_lock", StringComparison.Ordinal)) { attempted.TrySetResult(); }
        return ValueTask.FromResult(result);
    }
}
sealed class JwtConcurrencyContext(DbContextOptions<JwtConcurrencyContext> options, Func<Task>? beforeFirstSave = null, bool failAfterSaveOnce = false) : DbContext(options), IApplicationDbContext
{
    public int SaveCalls { get; private set; }
    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => Database.BeginTransactionAsync(cancellationToken);
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new JwtSigningKeyConfiguration());
        builder.Entity<JwtSigningKey>().Ignore(key => key.DomainEvents);
        builder.Entity<JwtSigningKey>().Ignore(key => key.IntegrationEvents);
    }
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (++SaveCalls == 1 && beforeFirstSave is not null) { await beforeFirstSave(); }
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries<EntityBase<Guid>>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified) { entry.Property(nameof(EntityBase<Guid>.Version)).CurrentValue = entry.Entity.Version + 1; }
        }
        var saved = await base.SaveChangesAsync(cancellationToken);
        if (failAfterSaveOnce && SaveCalls == 1) { throw new Npgsql.NpgsqlException("Synthetic transient failure after save", new IOException()); }
        return saved;
    }
}

public sealed class JwtKeyConcurrencyFixture : IAsyncLifetime
{
    private EconomyPostgreSqlTestDatabase? database;
    public string ConnectionString => database?.ConnectionString ?? throw new InvalidOperationException("Fixture is not initialized.");
    public async Task InitializeAsync()
    {
        database = await EconomyPostgreSqlTestDatabase.CreateEmptyAsync("jwt_key_concurrency");
        await using var context = new JwtConcurrencyContext(new DbContextOptionsBuilder<JwtConcurrencyContext>().UseNpgsql(ConnectionString).Options);
        await context.Database.EnsureCreatedAsync();
    }
    public async Task DisposeAsync()
    {
        if (database is not null) { await database.DisposeAsync(); }
    }
}
