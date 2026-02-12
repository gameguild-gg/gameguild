using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Services;

public class UsageTrackingServiceTests
{
    [Fact]
    public async Task TrackUsageAsync_Should_Save_Record()
    {
        await using var context = CreateContext();
        var service = new UsageTrackingService(context);
        var usage = new UsageTracking
        {
            TenantId = Guid.NewGuid(),
            Date = DateTime.UtcNow,
            ResourceType = "api",
            UsageAmount = 5,
            Cost = 1.25m
        };

        var id = await service.TrackUsageAsync(usage);

        id.Should().NotBeEmpty();
        (await context.UsageTracking.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetUsageAsync_Should_Filter_By_Range_And_ResourceType()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();

        context.UsageTracking.AddRange(
            new UsageTracking { TenantId = tenantId, Date = DateTime.UtcNow.AddDays(-1), ResourceType = "api", UsageAmount = 1 },
            new UsageTracking { TenantId = tenantId, Date = DateTime.UtcNow.AddDays(-2), ResourceType = "storage", UsageAmount = 2 },
            new UsageTracking { TenantId = Guid.NewGuid(), Date = DateTime.UtcNow.AddDays(-1), ResourceType = "api", UsageAmount = 3 }
        );
        await context.SaveChangesAsync();

        var service = new UsageTrackingService(context);

        var results = await service.GetUsageAsync(
            tenantId,
            DateTime.UtcNow.AddDays(-3),
            DateTime.UtcNow,
            resourceType: "api");

        results.Should().HaveCount(1);
        results[0].ResourceType.Should().Be("api");
    }

    [Fact]
    public async Task GetUsageSummaryAsync_Should_Aggregate_Data()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        context.UsageTracking.AddRange(
            new UsageTracking { TenantId = tenantId, Date = DateTime.UtcNow.AddDays(-1), ResourceType = "api", UsageAmount = 3, Cost = 2.5m },
            new UsageTracking { TenantId = tenantId, Date = DateTime.UtcNow.AddDays(-1), ResourceType = "storage", UsageAmount = 10, Cost = 5m },
            new UsageTracking { TenantId = tenantId, Date = DateTime.UtcNow.AddDays(-1), ResourceType = "api", UsageAmount = 2, Cost = 1m }
        );
        await context.SaveChangesAsync();

        var service = new UsageTrackingService(context);

        var summary = await service.GetUsageSummaryAsync(tenantId, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow);

        summary.TotalActions.Should().Be(3);
        summary.TotalCost.Should().Be(8.5m);
        summary.ActionCounts["api"].Should().Be(2);
        summary.ResourceCounts["api"].Should().Be(5);
        summary.ResourceCosts["storage"].Should().Be(5m);
    }

    [Fact]
    public async Task CleanupOldUsageDataAsync_Should_SoftDelete_Records()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var oldRecord = new UsageTracking { TenantId = tenantId, Date = DateTime.UtcNow.AddDays(-30), ResourceType = "api" };
        var recentRecord = new UsageTracking { TenantId = tenantId, Date = DateTime.UtcNow.AddDays(-1), ResourceType = "api" };

        context.UsageTracking.AddRange(oldRecord, recentRecord);
        await context.SaveChangesAsync();

        typeof(EntityBase).GetProperty(nameof(EntityBase.Version))!.SetValue(oldRecord, 1);

        var service = new UsageTrackingService(context);

        var deletedCount = await service.CleanupOldUsageDataAsync(DateTime.UtcNow.AddDays(-7));

        deletedCount.Should().Be(1);
        oldRecord.DeletedAt.Should().NotBeNull();
        recentRecord.DeletedAt.Should().BeNull();
    }

    private static TestUsageDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestUsageDbContext>()
            .UseInMemoryDatabase($"UsageTracking_{Guid.NewGuid()}")
            .Options;

        return new TestUsageDbContext(options);
    }

    private sealed class TestUsageDbContext(DbContextOptions<TestUsageDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<UsageTracking> UsageTracking { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<UsageTracking>(builder =>
            {
                builder.HasKey(ut => ut.Id);
                builder.Property(ut => ut.TenantId).IsRequired();
                builder.Property(ut => ut.ResourceType).IsRequired();
            });
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Mock.Of<IDbContextTransaction>());
        }
    }
}
