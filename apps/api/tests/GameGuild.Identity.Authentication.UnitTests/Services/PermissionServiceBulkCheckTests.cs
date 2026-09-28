using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
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

    [Fact]
    public async Task BulkCheckPermissionsAsync_EmitsAggregateMetricsWithoutRequestTags()
    {
        var options = new DbContextOptionsBuilder<PermissionServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var context = new PermissionServiceDbContext(options);
        var request = new BulkPermissionCheckRequest(Guid.NewGuid(), Guid.NewGuid(), PermissionType.Read);
        var measurements = new ConcurrentDictionary<string, long>();
        var durationRecorded = 0;
        var taggedMeasurementObserved = 0;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == "GameGuild.Identity.Authentication.PermissionBulkCheck")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            measurements.AddOrUpdate(instrument.Name, measurement, (_, current) => current + measurement);
            if (!tags.IsEmpty)
            {
                Interlocked.Exchange(ref taggedMeasurementObserved, 1);
            }
        });
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "permission_bulk_check_batch_duration" && measurement >= 0)
            {
                Interlocked.Exchange(ref durationRecorded, 1);
            }

            if (!tags.IsEmpty)
            {
                Interlocked.Exchange(ref taggedMeasurementObserved, 1);
            }
        });
        listener.Start();

        var results = await new PermissionService(context).BulkCheckPermissionsAsync([request, request]);

        results.Should().HaveCount(2);
        measurements.GetValueOrDefault("permission_bulk_check_batches").Should().BeGreaterThan(0);
        measurements.GetValueOrDefault("permission_bulk_check_requests").Should().BeGreaterThanOrEqualTo(2);
        measurements.GetValueOrDefault("permission_bulk_check_unique_requests").Should().BeGreaterThan(0);
        durationRecorded.Should().Be(1);
        taggedMeasurementObserved.Should().Be(0);
    }

    [Fact]
    public async Task BulkCheckPermissionsAsync_DeniesOverrideGlobalTenantAndUserAllows()
    {
        var options = new DbContextOptionsBuilder<PermissionServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var context = new PermissionServiceDbContext(options);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        context.Set<TenantPermission>().AddRange(
            new TenantPermission
            {
                UserId = null,
                TenantId = null,
                Permissions = [nameof(PermissionType.Read), nameof(PermissionType.Comment)],
                DenyPermissions = [nameof(PermissionType.Create)]
            },
            new TenantPermission
            {
                UserId = null,
                TenantId = tenantId,
                Permissions = [nameof(PermissionType.Create), nameof(PermissionType.Comment), nameof(PermissionType.Publish)],
                DenyPermissions = [nameof(PermissionType.Comment)]
            },
            new TenantPermission
            {
                UserId = userId,
                TenantId = tenantId,
                Permissions = [nameof(PermissionType.Create), nameof(PermissionType.Edit)],
                DenyPermissions = [nameof(PermissionType.Read), nameof(PermissionType.Publish)]
            });
        await context.SaveChangesAsync();

        var service = new PermissionService(context);
        var result = await service.BulkCheckPermissionsAsync(
            [userId, otherUserId],
            tenantId,
            [PermissionType.Read, PermissionType.Create, PermissionType.Comment, PermissionType.Publish, PermissionType.Edit, PermissionType.Delete]);

        result[userId].Should().BeEquivalentTo(new Dictionary<PermissionType, bool>
        {
            [PermissionType.Read] = false,
            [PermissionType.Create] = false,
            [PermissionType.Comment] = false,
            [PermissionType.Publish] = false,
            [PermissionType.Edit] = true,
            [PermissionType.Delete] = false
        });
        result[otherUserId].Should().BeEquivalentTo(new Dictionary<PermissionType, bool>
        {
            [PermissionType.Read] = true,
            [PermissionType.Create] = false,
            [PermissionType.Comment] = false,
            [PermissionType.Publish] = true,
            [PermissionType.Edit] = false,
            [PermissionType.Delete] = false
        });
    }

    [Fact]
    public async Task BulkCheckPermissionsAsync_EvaluatesTenantContentTypeAndResourceCombinations()
    {
        var options = new DbContextOptionsBuilder<PermissionServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var context = new PermissionServiceDbContext(options);
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        context.Set<TenantPermission>().AddRange(
            new TenantPermission { UserId = null, TenantId = null, Permissions = [nameof(PermissionType.Read)] },
            new TenantPermission { UserId = null, TenantId = tenantId, Permissions = [nameof(PermissionType.Create)] },
            new TenantPermission
            {
                UserId = userId,
                TenantId = tenantId,
                Permissions = [nameof(PermissionType.Comment)],
                DenyPermissions = [nameof(PermissionType.Create)]
            });
        var contentTypeGrant = new ContentTypePermission(userId: null, tenantId: null, contentTypeName: "Project");
        contentTypeGrant.SetPermissions([PermissionType.Edit]);
        context.Set<ContentTypePermission>().Add(contentTypeGrant);
        var resourceGrant = new GenericResourcePermission(userId, tenantId, resourceId, "Project");
        resourceGrant.SetPermissions([PermissionType.Publish]);
        context.Set<GenericResourcePermission>().Add(resourceGrant);
        await context.SaveChangesAsync();

        var service = new PermissionService(context);
        var requests = new[]
        {
            new BulkPermissionCheckRequest(userId, tenantId, PermissionType.Read),
            new BulkPermissionCheckRequest(userId, tenantId, PermissionType.Create),
            new BulkPermissionCheckRequest(userId, otherTenantId, PermissionType.Create),
            new BulkPermissionCheckRequest(otherUserId, tenantId, PermissionType.Comment),
            new BulkPermissionCheckRequest(userId, tenantId, PermissionType.Edit, ContentTypeName: "Project"),
            new BulkPermissionCheckRequest(userId, tenantId, PermissionType.Publish, ResourceId: resourceId, ResourceTypeName: "Project"),
            new BulkPermissionCheckRequest(userId, tenantId, PermissionType.Publish, ResourceId: resourceId, ResourceTypeName: "Course")
        };

        var results = await service.BulkCheckPermissionsAsync(requests);

        results.Select(result => result.IsGranted).Should().Equal(true, false, false, false, true, true, false);
        results.Select(result => result.Request).Should().Equal(requests);
    }

    [Fact]
    public async Task BulkCheckPermissionsAsync_EvaluatesMultipleResourcesAndPermissionsInOneBatch()
    {
        var options = new DbContextOptionsBuilder<PermissionServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var context = new PermissionServiceDbContext(options);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var firstResourceId = Guid.NewGuid();
        var secondResourceId = Guid.NewGuid();

        var firstResourceGrant = new GenericResourcePermission(userId, tenantId, firstResourceId, "Project");
        firstResourceGrant.SetPermissions([PermissionType.Read, PermissionType.Edit]);
        var secondResourceGrant = new GenericResourcePermission(userId, tenantId, secondResourceId, "Project");
        secondResourceGrant.SetPermissions([PermissionType.Read]);
        context.Set<GenericResourcePermission>().AddRange(firstResourceGrant, secondResourceGrant);
        await context.SaveChangesAsync();

        var requests = new[]
        {
            new BulkPermissionCheckRequest(userId, tenantId, PermissionType.Read, ResourceId: firstResourceId, ResourceTypeName: "Project"),
            new BulkPermissionCheckRequest(userId, tenantId, PermissionType.Edit, ResourceId: firstResourceId, ResourceTypeName: "Project"),
            new BulkPermissionCheckRequest(userId, tenantId, PermissionType.Delete, ResourceId: firstResourceId, ResourceTypeName: "Project"),
            new BulkPermissionCheckRequest(userId, tenantId, PermissionType.Read, ResourceId: secondResourceId, ResourceTypeName: "Project"),
            new BulkPermissionCheckRequest(userId, tenantId, PermissionType.Edit, ResourceId: secondResourceId, ResourceTypeName: "Project"),
            new BulkPermissionCheckRequest(otherUserId, tenantId, PermissionType.Read, ResourceId: firstResourceId, ResourceTypeName: "Project"),
            new BulkPermissionCheckRequest(userId, tenantId, PermissionType.Read, ResourceId: firstResourceId, ResourceTypeName: "Course")
        };

        var results = await new PermissionService(context).BulkCheckPermissionsAsync(requests);

        results.Select(result => result.IsGranted).Should().Equal(true, true, false, true, false, false, false);
        results.Select(result => result.Request).Should().Equal(requests);
    }

    [Fact]
    public async Task StreamBulkCheckPermissionsAsync_ProcessesBoundedBatchesAndPreservesInputOrder()
    {
        var options = new DbContextOptionsBuilder<PermissionServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var context = new PermissionServiceDbContext(options);
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        context.Set<TenantPermission>().Add(new TenantPermission
        {
            UserId = userId,
            TenantId = tenantId,
            Permissions = [nameof(PermissionType.Read)]
        });
        await context.SaveChangesAsync();

        var requests = new[]
        {
            new BulkPermissionCheckRequest(userId, tenantId, PermissionType.Read),
            new BulkPermissionCheckRequest(userId, tenantId, PermissionType.Delete),
            new BulkPermissionCheckRequest(userId, tenantId, PermissionType.Read)
        };
        var results = new List<BulkPermissionCheckResult>();
        await foreach (var result in new PermissionService(context).StreamBulkCheckPermissionsAsync(AsAsyncEnumerable(requests), batchSize: 2))
        {
            results.Add(result);
        }

        results.Select(result => result.IsGranted).Should().Equal(true, false, true);
        results.Select(result => result.Request).Should().Equal(requests);
    }

    [Fact]
    public async Task StreamBulkCheckPermissionsAsync_EvaluatesLargeBatchesInParallelAndPreservesInputOrder()
    {
        var options = new DbContextOptionsBuilder<PermissionServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var context = new PermissionServiceDbContext(options);
        var tenantId = Guid.NewGuid();
        var userIds = Enumerable.Range(0, 1_024).Select(_ => Guid.NewGuid()).ToArray();
        context.Set<TenantPermission>().Add(new TenantPermission
        {
            UserId = null,
            TenantId = tenantId,
            Permissions = [nameof(PermissionType.Read)]
        });
        context.Set<TenantPermission>().AddRange(userIds
            .Where((_, index) => index % 2 == 0)
            .Select(userId => new TenantPermission
            {
                UserId = userId,
                TenantId = tenantId,
                DenyPermissions = [nameof(PermissionType.Read)]
            }));
        await context.SaveChangesAsync();

        var requests = userIds
            .Select(userId => new BulkPermissionCheckRequest(userId, tenantId, PermissionType.Read))
            .ToArray();
        var results = new List<BulkPermissionCheckResult>();

        await foreach (var result in new PermissionService(context).StreamBulkCheckPermissionsAsync(AsAsyncEnumerable(requests), batchSize: 128))
        {
            results.Add(result);
        }

        results.Should().HaveCount(requests.Length);
        results.Select(result => result.Request).Should().Equal(requests);
        results.Select(result => result.IsGranted).Should().Equal(
            Enumerable.Range(0, requests.Length).Select(index => index % 2 != 0));
    }

    [Fact]
    public async Task StreamBulkCheckPermissionsAsync_RejectsUnboundedBatchesAndAmbiguousResourceTypes()
    {
        var options = new DbContextOptionsBuilder<PermissionServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var context = new PermissionServiceDbContext(options);
        var service = new PermissionService(context);
        var request = new BulkPermissionCheckRequest(Guid.NewGuid(), Guid.NewGuid(), PermissionType.Read);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await foreach (var _ in service.StreamBulkCheckPermissionsAsync(AsAsyncEnumerable([request]), batchSize: 257))
            {
                Assert.Fail("An invalid batch size must fail before yielding any permission decision.");
            }
        });
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            var ambiguousResourceRequest = request with { ResourceId = Guid.NewGuid() };
            await foreach (var _ in service.StreamBulkCheckPermissionsAsync(AsAsyncEnumerable([ambiguousResourceRequest])))
            {
                Assert.Fail("An ambiguous resource request must fail before yielding a permission decision.");
            }
        });
    }

    private static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(IEnumerable<T> values)
    {
        foreach (var value in values)
        {
            yield return value;
            await Task.CompletedTask;
        }
    }

    private sealed class PermissionServiceDbContext(DbContextOptions<PermissionServiceDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TenantPermission>().Ignore(permission => permission.Metadata);
            modelBuilder.Entity<ContentTypePermission>();
            modelBuilder.Entity<GenericResourcePermission>();
        }
    }
}
