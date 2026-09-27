using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authorization;
using Microsoft.Extensions.DependencyInjection;
using AuthenticationPermissionService = GameGuild.Identity.Authentication.PermissionService;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class BulkPermissionChecksPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
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
}
