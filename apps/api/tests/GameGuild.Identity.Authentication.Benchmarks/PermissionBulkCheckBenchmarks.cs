using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using GameGuild;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameGuild.Identity.Authentication.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class PermissionBulkCheckBenchmarks
{
    private PermissionBenchmarkDbContext _context = null!;
    private PermissionService _service = null!;
    private Guid _tenantId;
    private Guid[] _userIds = [];
    private BulkPermissionCheckRequest[] _requests = [];

    [Params(100, 1_000)]
    public int UserCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _tenantId = Guid.NewGuid();
        _userIds = Enumerable.Range(0, UserCount).Select(_ => Guid.NewGuid()).ToArray();
        _requests = _userIds
            .Select(userId => new BulkPermissionCheckRequest(userId, _tenantId, PermissionType.Read))
            .ToArray();

        var options = new DbContextOptionsBuilder<PermissionBenchmarkDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        _context = new PermissionBenchmarkDbContext(options);
        _context.Set<TenantPermission>().AddRange(_userIds
            .Where((_, index) => index % 2 == 0)
            .Select(userId => new TenantPermission
            {
                UserId = userId,
                TenantId = _tenantId,
                Permissions = [nameof(PermissionType.Read)]
            }));
        _context.SaveChanges();
        _service = new PermissionService(_context);
    }

    [IterationSetup]
    public void ResetTracking() => _context.ChangeTracker.Clear();

    [Benchmark(Baseline = true)]
    public async Task<int> IndividualChecks()
    {
        var granted = 0;
        foreach (var userId in _userIds)
        {
            if (await _service.HasTenantPermissionAsync(userId, _tenantId, PermissionType.Read).ConfigureAwait(false))
            {
                granted++;
            }
        }

        return granted;
    }

    [Benchmark]
    public async Task<int> BulkMatrixCheck()
    {
        var result = await _service.BulkCheckPermissionsAsync(_userIds, _tenantId, [PermissionType.Read]).ConfigureAwait(false);
        return result.Count(user => user.Value[PermissionType.Read]);
    }

    [Benchmark]
    public async Task<int> StreamedCombinationChecks()
    {
        var granted = 0;
        await foreach (var result in _service.StreamBulkCheckPermissionsAsync(AsAsyncEnumerable(_requests), batchSize: 128).ConfigureAwait(false))
        {
            if (result.IsGranted)
            {
                granted++;
            }
        }

        return granted;
    }

    [GlobalCleanup]
    public void Cleanup() => _context.Dispose();

    private static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(IEnumerable<T> values)
    {
        foreach (var value in values)
        {
            yield return value;
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    private sealed class PermissionBenchmarkDbContext(DbContextOptions<PermissionBenchmarkDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<TenantPermission>().Ignore(permission => permission.Metadata);
    }
}
