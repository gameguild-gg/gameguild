using GameGuild.Identity.Authorization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class PermissionServiceBulkCheckTests
{
    [Fact]
    public async Task BulkCheckPermissionsAsync_CombinesDefaultsAndActiveUserGrantsInOneResultPerUser()
    {
        var options = new DbContextOptionsBuilder<PermissionServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var context = new PermissionServiceDbContext(options);
        var tenantId = Guid.NewGuid();
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();

        context.Set<TenantPermission>().AddRange(
            new TenantPermission { UserId = null, TenantId = null, Permissions = [nameof(PermissionType.Read)] },
            new TenantPermission { UserId = null, TenantId = tenantId, Permissions = [nameof(PermissionType.Create)] },
            new TenantPermission { UserId = firstUserId, TenantId = tenantId, Permissions = [nameof(PermissionType.Comment)] },
            new TenantPermission
            {
                UserId = secondUserId,
                TenantId = tenantId,
                Permissions = [nameof(PermissionType.Delete)],
                ExpiresAt = SystemClock.UtcNow.AddMinutes(-1)
            },
            new TenantPermission
            {
                UserId = secondUserId,
                TenantId = tenantId,
                Permissions = [nameof(PermissionType.Follow)],
                IsActive = false
            },
            new TenantPermission
            {
                UserId = secondUserId,
                TenantId = null,
                Permissions = [nameof(PermissionType.Report)]
            });
        await context.SaveChangesAsync();

        var service = new PermissionService(context);
        var result = await service.BulkCheckPermissionsAsync(
            [firstUserId, secondUserId, firstUserId],
            tenantId,
            [PermissionType.Read, PermissionType.Create, PermissionType.Comment, PermissionType.Delete, PermissionType.Follow, PermissionType.Report]);

        result.Should().HaveCount(2);
        result[firstUserId].Should().BeEquivalentTo(new Dictionary<PermissionType, bool>
        {
            [PermissionType.Read] = true,
            [PermissionType.Create] = true,
            [PermissionType.Comment] = true,
            [PermissionType.Delete] = false,
            [PermissionType.Follow] = false,
            [PermissionType.Report] = false
        });
        result[secondUserId].Should().BeEquivalentTo(new Dictionary<PermissionType, bool>
        {
            [PermissionType.Read] = true,
            [PermissionType.Create] = true,
            [PermissionType.Comment] = false,
            [PermissionType.Delete] = false,
            [PermissionType.Follow] = false,
            [PermissionType.Report] = false
        });
    }

    [Fact]
    public async Task BulkCheckPermissionsAsync_HandlesEmptyUsersAndPermissionsWithoutQueryingGrants()
    {
        var options = new DbContextOptionsBuilder<PermissionServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var context = new PermissionServiceDbContext(options);
        var service = new PermissionService(context);
        var userId = Guid.NewGuid();

        (await service.BulkCheckPermissionsAsync([], Guid.NewGuid(), [PermissionType.Read])).Should().BeEmpty();
        (await service.BulkCheckPermissionsAsync([userId], Guid.NewGuid(), [])).Should().ContainKey(userId).WhoseValue.Should().BeEmpty();
    }

    private sealed class PermissionServiceDbContext(DbContextOptions<PermissionServiceDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TenantPermission>().Ignore(permission => permission.Metadata);
        }
    }
}
