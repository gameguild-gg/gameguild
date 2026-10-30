using System.Text.Json;
using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Configuration;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit;

namespace GameGuild.Identity.Authentication.IntegrationTests;

/// <summary>
///     Recovery and progress semantics of the automatic permission-expiration sweep
///     (issue #331 recovery review): backlogs larger than the worker's batch must
///     drain across cycles, and infrastructure failures must never strand a grant
///     without its audit trail or leave a tenant's cache version un-bumped.
///     Runs against a real PostgreSQL database with the real repository, audit
///     service and security-version store; only the failure injections are stubs.
/// </summary>
public sealed class PermissionExpirationRecoveryPostgreSqlTests
{
    [Fact]
    public async Task ExpiredSweepDrainsBacklogAcrossBatchesAndSkipsIneligibleGrants()
    {
        await using var harness = await ExpirationHarness.CreateAsync(batchSize: 2);

        var oldest = harness.SeedGrant(harness.TenantA, SystemClock.UtcNow.AddHours(-5));
        var second = harness.SeedGrant(harness.TenantA, SystemClock.UtcNow.AddHours(-4));
        var third = harness.SeedGrant(harness.TenantA, SystemClock.UtcNow.AddHours(-3));
        var fourth = harness.SeedGrant(harness.TenantA, SystemClock.UtcNow.AddHours(-2));
        var newest = harness.SeedGrant(harness.TenantB, SystemClock.UtcNow.AddHours(-1));

        // Ineligible rows must never occupy the batch window: processed (stamped +
        // deactivated) by an earlier cycle, soft-deleted, admin-deactivated before
        // expiry, or not yet expired.
        var processed = harness.SeedProcessedGrant(harness.TenantA, SystemClock.UtcNow.AddHours(-6));
        var deleted = harness.SeedGrant(harness.TenantA, SystemClock.UtcNow.AddHours(-7));
        var deactivated = harness.SeedGrant(harness.TenantA, SystemClock.UtcNow.AddHours(-8), isActive: false);
        var future = harness.SeedGrant(harness.TenantA, SystemClock.UtcNow.AddHours(2));
        await harness.SaveChangesAsync();
        await harness.Repository.DeleteAsync(deleted.Id);

        var cycleCounts = new List<int>();
        for (var cycle = 0; cycle < 8; cycle++)
        {
            var processedCount = await harness.SweepAsync();
            cycleCounts.Add(processedCount);
            if (processedCount == 0)
            {
                break;
            }
        }

        // BatchSize = 2 with five eligible grants: the backlog drains across cycles
        // instead of the first batch permanently occupying the window.
        cycleCounts.Should().Equal(2, 2, 1, 0);

        foreach (var grant in new[] { oldest, second, third, fourth, newest })
        {
            var reloaded = await harness.ReloadAsync(grant);
            reloaded.Should().NotBeNull();
            reloaded!.IsActive.Should().BeFalse();
            reloaded.Metadata.Should().ContainKey("expirationProcessedAt");
            (await harness.AuditCountForAsync(grant.Id)).Should().Be(1);
        }

        // Ineligible grants are untouched and unaudited.
        var reloadedProcessed = await harness.ReloadAsync(processed);
        reloadedProcessed!.IsActive.Should().BeFalse();
        reloadedProcessed.Metadata.Should().ContainKey("expirationProcessedAt");
        (await harness.AuditCountForAsync(processed.Id)).Should().Be(0);

        var reloadedDeleted = await harness.ReloadAsync(deleted);
        reloadedDeleted!.DeletedAt.Should().NotBeNull();
        (await harness.AuditCountForAsync(deleted.Id)).Should().Be(0);

        var reloadedDeactivated = await harness.ReloadAsync(deactivated);
        reloadedDeactivated!.IsActive.Should().BeFalse();
        reloadedDeactivated.Metadata.Should().BeNull();
        (await harness.AuditCountForAsync(deactivated.Id)).Should().Be(0);

        var reloadedFuture = await harness.ReloadAsync(future);
        reloadedFuture!.IsActive.Should().BeTrue();
        reloadedFuture.Metadata.Should().BeNull();
        (await harness.AuditCountForAsync(future.Id)).Should().Be(0);
    }

    [Fact]
    public async Task AuditFailureLeavesGrantUnchangedAndEligibleForRetry()
    {
        await using var harness = await ExpirationHarness.CreateAsync(batchSize: 10);
        var grant = harness.SeedGrant(harness.TenantA, SystemClock.UtcNow.AddHours(-1));
        await harness.SaveChangesAsync();

        var failingCycle = await harness.SweepAsync(auditOverride: harness.CreateFailingAudit());

        failingCycle.Should().Be(0);
        var afterFailure = await harness.ReloadAsync(grant);
        afterFailure!.IsActive.Should().BeTrue();
        afterFailure.Metadata.Should().BeNull();
        (await harness.AuditCountForAsync(grant.Id)).Should().Be(0);

        var retryCycle = await harness.SweepAsync();

        retryCycle.Should().Be(1);
        var afterRetry = await harness.ReloadAsync(grant);
        afterRetry!.IsActive.Should().BeFalse();
        afterRetry.Metadata.Should().ContainKey("expirationProcessedAt");
        (await harness.AuditCountForAsync(grant.Id)).Should().Be(1);
    }

    [Fact]
    public async Task PersistFailureAfterAuditKeepsAuditEntryAndConvergesOnRetry()
    {
        await using var harness = await ExpirationHarness.CreateAsync(batchSize: 10);
        var grant = harness.SeedGrant(harness.TenantA, SystemClock.UtcNow.AddHours(-1));
        await harness.SaveChangesAsync();

        var versionBefore = await harness.TenantVersionAsync(harness.TenantA);

        var failingCycle = await harness.SweepAsync(
            repositoryOverride: new FailingFirstUpdateRepository(harness.Repository));

        failingCycle.Should().Be(0);

        // The audit entry is written BEFORE the durable deactivation (and the tenant
        // version is bumped before any mutation), so a persistence failure strands
        // neither the audit trail nor the cache invalidation.
        (await harness.AuditCountForAsync(grant.Id)).Should().Be(1);
        var afterFailure = await harness.ReloadAsync(grant);
        afterFailure!.IsActive.Should().BeTrue();
        (await harness.TenantVersionAsync(harness.TenantA)).Should().BeGreaterThan(versionBefore);

        var retryCycle = await harness.SweepAsync();

        retryCycle.Should().Be(1);
        var afterRetry = await harness.ReloadAsync(grant);
        afterRetry!.IsActive.Should().BeFalse();
        afterRetry.Metadata.Should().ContainKey("expirationProcessedAt");
        // The retry may duplicate the audit entry, but it is never lost.
        (await harness.AuditCountForAsync(grant.Id)).Should().BeGreaterThanOrEqualTo(2);
        (await harness.TenantVersionAsync(harness.TenantA)).Should().BeGreaterThan(versionBefore + 1);
    }

    private sealed class ExpirationHarness : IAsyncDisposable
    {
        private readonly ExpirationRecoveryDbContext _context;
        private readonly EconomyPostgreSqlTestDatabase _database;

        private ExpirationHarness(ExpirationRecoveryDbContext context, EconomyPostgreSqlTestDatabase database)
        {
            _context = context;
            _database = database;
        }

        public Guid TenantA { get; } = Guid.NewGuid();

        public Guid TenantB { get; } = Guid.NewGuid();

        public TenantPermissionRepository Repository { get; private set; } = null!;

        public PermissionAuditService AuditService { get; private set; } = null!;

        public PermissionExpirationOptions Options { get; private set; } = null!;

        public static async Task<ExpirationHarness> CreateAsync(int batchSize)
        {
            var database = await EconomyPostgreSqlTestDatabase.CreateAsync("permission_expiration_recovery");
            var connectionString = new NpgsqlConnectionStringBuilder(database.ConnectionString)
            {
                Pooling = true
            }.ConnectionString;
            var options = new DbContextOptionsBuilder<ExpirationRecoveryDbContext>()
                .UseNpgsql(connectionString)
                .Options;
            var context = new ExpirationRecoveryDbContext(options);
            await context.Database.EnsureCreatedAsync();

            return new ExpirationHarness(context, database)
            {
                Repository = new TenantPermissionRepository(context),
                AuditService = new PermissionAuditService(
                    new PermissionAuditLogRepository(context),
                    NullLogger<PermissionAuditService>.Instance),
                Options = new PermissionExpirationOptions
                {
                    BatchSize = batchSize,
                    Enabled = true
                }
            };
        }

        public TenantPermission SeedGrant(Guid tenantId, DateTime expiresAt, bool isActive = true)
        {
            var grant = new TenantPermission
            {
                UserId = Guid.NewGuid(),
                TenantId = tenantId,
                Permissions = ["Read"],
                ExpiresAt = expiresAt,
                IsActive = isActive
            };
            _context.Set<TenantPermission>().Add(grant);
            return grant;
        }

        public TenantPermission SeedProcessedGrant(Guid tenantId, DateTime expiresAt)
        {
            var grant = new TenantPermission
            {
                UserId = Guid.NewGuid(),
                TenantId = tenantId,
                Permissions = ["Read"],
                ExpiresAt = expiresAt,
                IsActive = false,
                Metadata = new Dictionary<string, object>
                {
                    ["expirationProcessedAt"] = SystemClock.UtcNow.AddHours(-1).ToString("O")
                }
            };
            _context.Set<TenantPermission>().Add(grant);
            return grant;
        }

        public async Task<int> SaveChangesAsync()
        {
            var written = await _context.SaveChangesAsync();
            // Detach the seeds so the sweep (which re-reads grants with AsNoTracking
            // and re-attaches them via Update) never hits a duplicate-key tracking
            // conflict against the seeded instances.
            _context.ChangeTracker.Clear();
            return written;
        }

        public async Task<int> SweepAsync(
            ITenantPermissionRepository? repositoryOverride = null,
            IPermissionAuditService? auditOverride = null)
        {
            var service = new PermissionExpirationService(
                repositoryOverride ?? Repository,
                auditOverride ?? AuditService,
                new DatabaseTenantSecurityVersionStore(new TenantSecurityVersionRepository(_context)),
                global::Microsoft.Extensions.Options.Options.Create(Options),
                NullLogger<PermissionExpirationService>.Instance);
            return await service.ProcessExpiredAsync();
        }

        /// <summary>
        ///     Fails every audit write; proves an audit outage leaves the grant fully
        ///     eligible for the next cycle instead of persisting first and losing the
        ///     audit entry.
        /// </summary>
        public IPermissionAuditService CreateFailingAudit() => new FailingAuditService();

        public async Task<TenantPermission?> ReloadAsync(TenantPermission grant)
        {
            return await _context.Set<TenantPermission>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == grant.Id);
        }

        public async Task<int> AuditCountForAsync(Guid grantId)
        {
            return await _context.Set<PermissionAuditLog>()
                .AsNoTracking()
                .CountAsync(l => l.OperationType == PermissionOperationType.Expire && l.ResourceId == grantId);
        }

        public async Task<long> TenantVersionAsync(Guid tenantId)
        {
            var version = await _context.Set<TenantSecurityVersion>()
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.TenantId == tenantId);
            return version?.SecurityVersion ?? 0;
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
            await _database.DisposeAsync();
        }

        private sealed class FailingAuditService : IPermissionAuditService
        {
            public Task<PermissionAuditLog> LogPermissionChangeAsync(
                PermissionOperationType operationType,
                Guid? userId,
                Guid performedBy,
                Guid? tenantId,
                string? permissionType = null,
                Guid? resourceId = null,
                string? resourceType = null,
                string? oldValue = null,
                string? newValue = null,
                string? reason = null,
                bool success = true,
                string? errorMessage = null,
                string? ipAddress = null,
                string? userAgent = null,
                CancellationToken cancellationToken = default) =>
                Task.FromException<PermissionAuditLog>(new InvalidOperationException("audit unavailable"));

            public Task<List<PermissionAuditLog>> GetAuditLogsAsync(Guid? tenantId, DateTime? fromDate = null, DateTime? toDate = null, int? limit = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<List<PermissionAuditLog>> GetUserAuditHistoryAsync(Guid userId, Guid? tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<List<PermissionAuditLog>> GetResourceAuditHistoryAsync(Guid resourceId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<List<PermissionAuditLog>> GetAuditLogsByOperationAsync(PermissionOperationType operationType, Guid? tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<List<PermissionAuditLog>> GetFailedOperationsAsync(Guid? tenantId, DateTime? fromDate = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }
    }

    /// <summary>
    ///     Delegating repository whose first UpdateAsync fails, proving that a grant
    ///     whose audit entry was already persisted converges on the next cycle.
    /// </summary>
    private sealed class FailingFirstUpdateRepository(ITenantPermissionRepository inner) : ITenantPermissionRepository
    {
        private int _updateCalls;

        public Task<TenantPermission> CreateAsync(TenantPermission permission, CancellationToken cancellationToken = default) => inner.CreateAsync(permission, cancellationToken);

        public async Task<TenantPermission> UpdateAsync(TenantPermission permission, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _updateCalls) == 1)
            {
                throw new InvalidOperationException("database unavailable");
            }

            return await inner.UpdateAsync(permission, cancellationToken);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => inner.DeleteAsync(id, cancellationToken);

        public Task<TenantPermission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => inner.GetByIdAsync(id, cancellationToken);

        public Task<TenantPermission?> GetByUserAndTenantAsync(Guid? userId, Guid? tenantId, CancellationToken cancellationToken = default) => inner.GetByUserAndTenantAsync(userId, tenantId, cancellationToken);

        public Task<List<TenantPermission>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) => inner.GetByTenantAsync(tenantId, cancellationToken);

        public Task<List<TenantPermission>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) => inner.GetByUserAsync(userId, cancellationToken);

        public Task<List<TenantPermission>> GetExpiredPermissionsAsync(CancellationToken cancellationToken = default) => inner.GetExpiredPermissionsAsync(cancellationToken);

        public Task<TenantPermission?> GetDeletedByIdAsync(Guid id, CancellationToken cancellationToken = default) => inner.GetDeletedByIdAsync(id, cancellationToken);

        public Task<List<TenantPermission>> GetExpiringBeforeAsync(DateTime cutoff, CancellationToken cancellationToken = default) => inner.GetExpiringBeforeAsync(cutoff, cancellationToken);

        public Task<List<TenantPermission>> GetByIdsInTenantAsync(Guid tenantId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => inner.GetByIdsInTenantAsync(tenantId, ids, cancellationToken);
    }

    private sealed class ExpirationRecoveryDbContext(DbContextOptions<ExpirationRecoveryDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        DbSet<T> IApplicationDbContext.Set<T>() => Set<T>();

        Task<int> IApplicationDbContext.SaveChangesAsync(CancellationToken cancellationToken) =>
            SaveChangesAsync(cancellationToken);

        Task<IDbContextTransaction> IApplicationDbContext.BeginTransactionAsync(CancellationToken cancellationToken) =>
            Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new TenantSecurityVersionConfiguration());
            modelBuilder.ApplyConfiguration(new PermissionAuditLogConfiguration());

            // The production model stores Metadata as jsonb; the minimal harness maps
            // it through a JSON string converter so the dictionary round-trips on any
            // Npgsql configuration (stamp reads handle both string and JsonElement
            // values via PermissionExpirationService.TryGetStamp).
            modelBuilder.Entity<TenantPermission>()
                .Property(permission => permission.Metadata)
                .HasConversion(
                    value => value == null ? null : JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                    value => string.IsNullOrWhiteSpace(value)
                        ? null
                        : JsonSerializer.Deserialize<Dictionary<string, object>>(value));
        }
    }
}
