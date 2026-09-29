using System.Diagnostics;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using AuthenticationPermissionService = GameGuild.Identity.Authentication.PermissionService;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class BulkPermissionChecksPostgreSqlTests(ApiPostgreSqlFixture fixture, ITestOutputHelper output)
{
    [Theory]
    [InlineData(100)]
    [InlineData(1_000)]
    public async Task BulkCheckPermissionsAsync_CompareToIndividualPostgreSqlChecks(int userCount)
    {
        var tenantId = Guid.NewGuid();
        var userIds = Enumerable.Range(0, userCount).Select(_ => Guid.NewGuid()).ToArray();
        var expectedGrantCount = (userCount + 1) / 2;

        await using (var seedScope = fixture.Factory.Services.CreateAsyncScope())
        {
            var seedContext = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seedContext.Set<TenantPermission>().AddRange(userIds
                .Where((_, index) => index % 2 == 0)
                .Select(userId => new TenantPermission
                {
                    UserId = userId,
                    TenantId = tenantId,
                    Permissions = [nameof(PermissionType.Read)]
                }));
            await seedContext.SaveChangesAsync();
        }

        try
        {
            var warmupUsers = userIds.Take(Math.Min(userCount, 8)).ToArray();
            await MeasureIndividualChecksAsync(warmupUsers, tenantId);
            await MeasureBulkChecksAsync(warmupUsers, tenantId);

            var individual = await MeasureIndividualChecksAsync(userIds, tenantId);
            var bulk = await MeasureBulkChecksAsync(userIds, tenantId);

            Assert.Equal(expectedGrantCount, individual.GrantedCount);
            Assert.Equal(individual.GrantedCount, bulk.GrantedCount);
            output.WriteLine(
                "PostgreSQL permission checks at {0:N0} users: individual={1:F1} ms, one-query bulk matrix={2:F1} ms, speedup={3:F2}x",
                userCount,
                individual.Elapsed.TotalMilliseconds,
                bulk.Elapsed.TotalMilliseconds,
                individual.Elapsed.TotalMilliseconds / Math.Max(0.1, bulk.Elapsed.TotalMilliseconds));
        }
        finally
        {
            await using var cleanupScope = fixture.Factory.Services.CreateAsyncScope();
            var cleanupContext = cleanupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await cleanupContext.Set<TenantPermission>()
                .Where(grant => grant.TenantId == tenantId)
                .ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task BulkCheckPermissionsAsync_HandlesConcurrentLargeBatchesAgainstPostgreSql()
    {
        const int userCount = 1_024;
        const int concurrentCallers = 8;
        const int usersPerCaller = userCount / concurrentCallers;
        var tenantId = Guid.NewGuid();
        var userIds = Enumerable.Range(0, userCount).Select(_ => Guid.NewGuid()).ToArray();
        var expectedGrants = userIds
            .Where((_, index) => index % 2 == 0)
            .ToHashSet();

        await using (var seedScope = fixture.Factory.Services.CreateAsyncScope())
        {
            var seedContext = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            seedContext.Set<TenantPermission>().AddRange(userIds.Select((userId, index) => new TenantPermission
            {
                UserId = userId,
                TenantId = tenantId,
                Permissions = [nameof(PermissionType.Read)],
                DenyPermissions = index % 2 == 0 ? [] : [nameof(PermissionType.Read)]
            }));
            await seedContext.SaveChangesAsync();
        }

        try
        {
            var callers = Enumerable.Range(0, concurrentCallers).Select(async callerIndex =>
            {
                await using var scope = fixture.Factory.Services.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var requests = userIds
                    .Skip(callerIndex * usersPerCaller)
                    .Take(usersPerCaller)
                    .Select(userId => new BulkPermissionCheckRequest(userId, tenantId, PermissionType.Read))
                    .ToArray();

                return await new AuthenticationPermissionService(context)
                    .BulkCheckPermissionsAsync(requests, CancellationToken.None);
            }).ToArray();

            var results = (await Task.WhenAll(callers))
                .SelectMany(result => result)
                .ToArray();

            Assert.Equal(userCount, results.Length);
            Assert.All(results, result =>
                Assert.Equal(expectedGrants.Contains(result.Request.UserId), result.IsGranted));
        }
        finally
        {
            await using var cleanupScope = fixture.Factory.Services.CreateAsyncScope();
            var cleanupContext = cleanupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await cleanupContext.Set<TenantPermission>()
                .Where(grant => grant.TenantId == tenantId)
                .ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task StreamBulkCheckPermissionsAsync_EvaluatesLargeBatchesAgainstPostgreSql()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var tenantId = Guid.NewGuid();
        var requests = Enumerable.Range(0, 1_024)
            .Select(_ => new BulkPermissionCheckRequest(Guid.NewGuid(), tenantId, PermissionType.Read))
            .ToArray();

        context.Set<TenantPermission>().AddRange(
            new TenantPermission
            {
                UserId = null,
                TenantId = tenantId,
                Permissions = [nameof(PermissionType.Read)]
            },
            new TenantPermission
            {
                UserId = requests[0].UserId,
                TenantId = tenantId,
                Permissions = [nameof(PermissionType.Comment)],
                DenyPermissions = [nameof(PermissionType.Read)]
            });
        await context.SaveChangesAsync();

        var results = new List<BulkPermissionCheckResult>();
        await foreach (var result in new AuthenticationPermissionService(context).StreamBulkCheckPermissionsAsync(
                           AsAsyncEnumerable(requests),
                           batchSize: 128))
        {
            results.Add(result);
        }

        Assert.Equal(requests.Length, results.Count);
        Assert.False(results[0].IsGranted, "a user-specific deny overrides the tenant default");
        Assert.All(results.Skip(1), result => Assert.True(result.IsGranted));
        await transaction.RollbackAsync();
    }

    private static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(IEnumerable<T> values)
    {
        foreach (var value in values)
        {
            yield return value;
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    private async Task<(TimeSpan Elapsed, int GrantedCount)> MeasureIndividualChecksAsync(Guid[] userIds, Guid tenantId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = new AuthenticationPermissionService(context);
        var grantedCount = 0;
        var stopwatch = Stopwatch.StartNew();

        foreach (var userId in userIds)
        {
            if (await service.HasTenantPermissionAsync(userId, tenantId, PermissionType.Read)) grantedCount++;
        }

        stopwatch.Stop();
        return (stopwatch.Elapsed, grantedCount);
    }

    private async Task<(TimeSpan Elapsed, int GrantedCount)> MeasureBulkChecksAsync(Guid[] userIds, Guid tenantId)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = new AuthenticationPermissionService(context);
        var stopwatch = Stopwatch.StartNew();
        var results = await service.BulkCheckPermissionsAsync(userIds, tenantId, [PermissionType.Read]);
        stopwatch.Stop();

        return (stopwatch.Elapsed, results.Count(user => user.Value[PermissionType.Read]));
    }
}
