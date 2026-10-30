using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Compliance.Audit;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class PermissionExpirationRecoveryPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task ProcessedFirstBatchDoesNotHideLaterTenantAndGlobalGrantsAcrossFreshScopes()
    {
        await using var host = await CreateHostAsync();
        var tenantA = Guid.NewGuid(); var tenantB = Guid.NewGuid();
        var grants = Enumerable.Range(1, 6).Select(index => Grant(
            index <= 2 ? tenantA : index <= 4 ? tenantB : null,
            Guid.Parse($"00000000-0000-0000-0000-{index:D12}"))).ToArray();
        grants[5].SoftDelete();
        var ids = grants.Select(grant => grant.Id).ToArray();
        var baselines = await SeedAsync(host.Factory, grants, [tenantA, tenantB, Guid.Empty]);

        // A stable ordering is a legal repository result. Keep it deterministic while
        // retaining the real repository's PostgreSQL query, filtering and writes.
        foreach (var expected in new[] { 2, 2, 1 })
        {
            await using var cycle = host.Factory.Services.CreateAsyncScope();
            Assert.Equal(expected, await Service(cycle.ServiceProvider, ids, batchSize: 2).ProcessExpiredAsync());
        }

        await using var verify = host.Factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var saved = await db.Set<TenantPermission>().IgnoreQueryFilters().AsNoTracking()
            .Where(grant => ids.Contains(grant.Id)).OrderBy(grant => grant.Id).ToArrayAsync();
        Assert.All(saved.Take(5), grant =>
        {
            Assert.False(grant.IsActive);
            Assert.True(grant.Metadata?.ContainsKey("expirationProcessedAt") == true);
        });
        Assert.True(saved[5].IsDeleted);
        Assert.False(saved[5].Metadata?.ContainsKey("expirationProcessedAt") == true);
        Assert.Equal(5, await db.Set<PermissionAuditLog>()
            .CountAsync(log => ids.Contains(log.ResourceId!.Value) && log.OperationType == PermissionOperationType.Expire));
        var versions = verify.ServiceProvider.GetRequiredService<ITenantSecurityVersionStore>();
        foreach (var baseline in baselines)
        {
            Assert.Equal(baseline.Value + 1, await versions.GetVersionAsync(baseline.Key.ToString("D")));
        }
    }

    [Theory]
    [InlineData("audit")]
    [InlineData("version")]
    public async Task FailureAfterProvisionalPersistenceRollsBackAndFreshScopeCanRetry(string boundary)
    {
        await using var host = await CreateHostAsync();
        var tenant = Guid.NewGuid(); var grant = Grant(tenant);
        var baseline = (await SeedAsync(host.Factory, [grant], [tenant]))[tenant];
        var reachedPersistedMutation = false;
        Exception? failure;
        await using (var scope = host.Factory.Services.CreateAsyncScope())
        {
            var services = scope.ServiceProvider;
            var db = services.GetRequiredService<ApplicationDbContext>();
            var realVersions = services.GetRequiredService<ITenantSecurityVersionStore>();
            var auditRepository = new Mock<IPermissionAuditLogRepository>(MockBehavior.Strict);
            auditRepository.Setup(repository => repository.CreateAsync(It.IsAny<PermissionAuditLog>(), It.IsAny<CancellationToken>()))
                .Returns(async (PermissionAuditLog _, CancellationToken token) =>
                {
                    reachedPersistedMutation = await db.Set<TenantPermission>().AsNoTracking()
                        .AnyAsync(row => row.Id == grant.Id && !row.IsActive, token);
                    throw new InjectedExpirationFailure();
                });
            var versionStore = new Mock<ITenantSecurityVersionStore>(MockBehavior.Strict);
            versionStore.Setup(store => store.IncrementVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(async (string key, CancellationToken token) =>
                {
                    await realVersions.IncrementVersionAsync(key, token);
                    reachedPersistedMutation = await db.Set<PermissionAuditLog>()
                        .AnyAsync(row => row.ResourceId == grant.Id && row.OperationType == PermissionOperationType.Expire, token);
                    throw new InjectedExpirationFailure();
                });
            var audit = boundary == "audit"
                ? new PermissionAuditService(auditRepository.Object, NullLogger<PermissionAuditService>.Instance)
                : services.GetRequiredService<IPermissionAuditService>();
            var service = Service(services, [grant.Id], audit,
                boundary == "version" ? versionStore.Object : realVersions);
            failure = await Record.ExceptionAsync(() => service.ProcessExpiredAsync());
        }

        Assert.True(reachedPersistedMutation);
        await AssertUnprocessedAsync(host.Factory, grant, baseline);
        Assert.NotNull(failure);
        await using (var retry = host.Factory.Services.CreateAsyncScope())
        {
            Assert.Equal(1, await Service(retry.ServiceProvider, [grant.Id]).ProcessExpiredAsync());
        }
        await using var verify = host.Factory.Services.CreateAsyncScope();
        var persisted = await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Set<TenantPermission>().AsNoTracking().SingleAsync(row => row.Id == grant.Id);
        Assert.False(persisted.IsActive);
        Assert.True(persisted.Metadata?.ContainsKey("expirationProcessedAt") == true);
        Assert.Equal(baseline + 1, await verify.ServiceProvider.GetRequiredService<ITenantSecurityVersionStore>()
            .GetVersionAsync(tenant.ToString("D")));
        Assert.Equal(1, await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Set<PermissionAuditLog>().CountAsync(row => row.ResourceId == grant.Id && row.OperationType == PermissionOperationType.Expire));
    }

    [Fact]
    public async Task GlobalExpirationAdvancesGlobalVersionAndKeepsNullAuditScope()
    {
        await using var host = await CreateHostAsync();
        var grant = Grant(null);
        var baseline = (await SeedAsync(host.Factory, [grant], [Guid.Empty]))[Guid.Empty];
        await using (var cycle = host.Factory.Services.CreateAsyncScope())
        {
            Assert.Equal(1, await Service(cycle.ServiceProvider, [grant.Id]).ProcessExpiredAsync());
        }
        await using var verify = host.Factory.Services.CreateAsyncScope();
        Assert.Equal(baseline + 1, await verify.ServiceProvider.GetRequiredService<ITenantSecurityVersionStore>()
            .GetVersionAsync(Guid.Empty.ToString("D")));
        var audit = await verify.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Set<PermissionAuditLog>().SingleAsync(row => row.ResourceId == grant.Id && row.OperationType == PermissionOperationType.Expire);
        Assert.Null(audit.TenantId);
        Assert.Equal(Guid.Empty, audit.PerformedBy);
    }

    [Fact]
    public async Task CancellationAfterAuditPersistenceDoesNotCommitCheckpointOrVersion()
    {
        await using var host = await CreateHostAsync();
        var tenant = Guid.NewGuid(); var grant = Grant(tenant);
        var baseline = (await SeedAsync(host.Factory, [grant], [tenant]))[tenant];
        using var cancellation = new CancellationTokenSource();
        await using (var scope = host.Factory.Services.CreateAsyncScope())
        {
            var services = scope.ServiceProvider;
            var real = services.GetRequiredService<IPermissionAuditLogRepository>();
            var repository = new Mock<IPermissionAuditLogRepository>(MockBehavior.Strict);
            repository.Setup(value => value.CreateAsync(It.IsAny<PermissionAuditLog>(), It.IsAny<CancellationToken>()))
                .Returns(async (PermissionAuditLog row, CancellationToken token) =>
                {
                    var saved = await real.CreateAsync(row, token);
                    cancellation.Cancel();
                    return saved;
                });
            var audit = new PermissionAuditService(repository.Object, NullLogger<PermissionAuditService>.Instance);
            var failure = await Record.ExceptionAsync(() => Service(services, [grant.Id], audit)
                .ProcessExpiredAsync(cancellation.Token));
            Assert.IsAssignableFrom<OperationCanceledException>(failure);
        }
        await AssertUnprocessedAsync(host.Factory, grant, baseline);
    }

    [Fact]
    public async Task AuditFailureRestoresItsSavepointAndPreservesEarlierCallerWorkAndEvidence()
    {
        await using var host = await CreateHostAsync();
        var tenant = Guid.NewGuid(); var grant = Grant(tenant);
        var baseline = (await SeedAsync(host.Factory, [grant], [tenant]))[tenant];
        var earlier = new TenantPermission
        {
            TenantId = Guid.NewGuid(), UserId = Guid.NewGuid(), Permissions = ["Read"], GrantedBy = Guid.NewGuid()
        };
        await using (var scope = host.Factory.Services.CreateAsyncScope())
        {
            var services = scope.ServiceProvider;
            var db = services.GetRequiredService<ApplicationDbContext>();
            await using var caller = await db.Database.BeginTransactionAsync();
            db.Add(earlier); await db.SaveChangesAsync();
            var auditRepository = new Mock<IPermissionAuditLogRepository>(MockBehavior.Strict);
            auditRepository.Setup(repository => repository.CreateAsync(It.IsAny<PermissionAuditLog>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InjectedExpirationFailure());
            var audit = new PermissionAuditService(auditRepository.Object, NullLogger<PermissionAuditService>.Instance);
            var failure = await Record.ExceptionAsync(() => Service(services, [grant.Id], audit).ProcessExpiredAsync());
            Assert.NotNull(failure);
            Assert.Same(caller, db.Database.CurrentTransaction);
            Assert.Equal(EntityState.Unchanged, db.Entry(earlier).State);
            Assert.True(await db.Set<TenantPermission>().AsNoTracking().AnyAsync(row => row.Id == earlier.Id));
            Assert.True((await db.Set<TenantPermission>().AsNoTracking().SingleAsync(row => row.Id == grant.Id)).IsActive);
            await caller.CommitAsync();
            await db.FlushPendingPermissionAuditChangesAsync();
        }
        await AssertUnprocessedAsync(host.Factory, grant, baseline);
        await using var verify = host.Factory.Services.CreateAsyncScope();
        var persisted = verify.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await persisted.Set<TenantPermission>().AnyAsync(row => row.Id == earlier.Id));
        Assert.Equal(1, await persisted.Set<AuditLog>().CountAsync(row =>
            row.ResourceId == earlier.Id.ToString() && row.Category == AuditCategory.Permission));
    }

    private static TenantPermission Grant(Guid? tenant, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(), UserId = tenant is null ? null : Guid.NewGuid(), TenantId = tenant,
        Permissions = ["Read"], GrantedBy = Guid.NewGuid(), ExpiresAt = DateTime.UtcNow.Date.AddDays(-1)
    };

    private static async Task<Dictionary<Guid, long>> SeedAsync(WebApplicationFactory<Program> factory,
        TenantPermission[] grants, Guid[] versionScopes)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.AddRange(grants); await db.SaveChangesAsync();
        var result = new Dictionary<Guid, long>();
        var versions = scope.ServiceProvider.GetRequiredService<ITenantSecurityVersionStore>();
        foreach (var key in versionScopes) { result[key] = await versions.IncrementVersionAsync(key.ToString("D")); }
        return result;
    }

    private static PermissionExpirationService Service(IServiceProvider services, Guid[] ids,
        IPermissionAuditService? audit = null, ITenantSecurityVersionStore? versions = null, int batchSize = 100)
    {
        var real = services.GetRequiredService<ITenantPermissionRepository>();
        var repository = new Mock<ITenantPermissionRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetExpiredPermissionsAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken token) => (await real.GetExpiredPermissionsAsync(token))
                .Where(grant => ids.Contains(grant.Id)).OrderBy(grant => grant.Id).ToList());
        repository.Setup(value => value.UpdateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .Returns((TenantPermission grant, CancellationToken token) => real.UpdateAsync(grant, token));
        var publisher = new Mock<IPublisher>(MockBehavior.Strict);
        publisher.Setup(value => value.Publish(It.IsAny<PermissionExpirationNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return new PermissionExpirationService(repository.Object, audit ?? services.GetRequiredService<IPermissionAuditService>(),
            versions ?? services.GetRequiredService<ITenantSecurityVersionStore>(),
            Options.Create(new PermissionExpirationOptions { BatchSize = batchSize }),
            NullLogger<PermissionExpirationService>.Instance, publisher.Object, services.GetRequiredService<IActorContextAccessor>());
    }

    private static async Task AssertUnprocessedAsync(WebApplicationFactory<Program> factory, TenantPermission original, long baseline)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var grant = await db.Set<TenantPermission>().AsNoTracking().SingleAsync(row => row.Id == original.Id);
        Assert.True(grant.IsActive);
        Assert.Equal(original.ExpiresAt, grant.ExpiresAt);
        Assert.False(grant.Metadata?.ContainsKey("expirationProcessedAt") == true);
        Assert.False(await db.Set<PermissionAuditLog>().AnyAsync(row => row.ResourceId == original.Id && row.OperationType == PermissionOperationType.Expire));
        Assert.Equal(baseline, await scope.ServiceProvider.GetRequiredService<ITenantSecurityVersionStore>()
            .GetVersionAsync((original.TenantId ?? Guid.Empty).ToString("D")));
    }

    private async Task<ExpirationHost> CreateHostAsync()
    {
        // The sweep is global; an isolated migrated database prevents cross-test expiration.
        var database = await EconomyPostgreSqlTestDatabase.CreateAsync("permission_expiration_recovery");
        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(database.ConnectionString, provider => provider.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)).Options;
            await using (var bootstrap = new ApplicationDbContext(options)) { await bootstrap.Database.MigrateAsync(); }
            var factory = fixture.CreateFactory(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = database.ConnectionString,
                    ["ConnectionStrings:MigrationConnection"] = database.ConnectionString,
                    ["Authorization:PermissionExpiration:Enabled"] = "false"
                }));
                builder.ConfigureTestServices(registrations =>
                {
                    registrations.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                    registrations.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                    registrations.AddDbContext<ApplicationDbContext>(dbOptions => dbOptions.UseNpgsql(database.ConnectionString,
                        provider => provider.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
                });
            });
            return new ExpirationHost(database, factory);
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    private sealed class ExpirationHost(EconomyPostgreSqlTestDatabase database, WebApplicationFactory<Program> factory) : IAsyncDisposable
    {
        public WebApplicationFactory<Program> Factory => factory;

        public async ValueTask DisposeAsync()
        {
            try { factory.Dispose(); }
            finally { await database.DisposeAsync(); }
        }
    }

    private sealed class InjectedExpirationFailure : Exception { }
}
