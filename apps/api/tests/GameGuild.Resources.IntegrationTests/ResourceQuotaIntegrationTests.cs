using FluentAssertions;
using GameGuild.Abstractions;
using GameGuild.API.Database;
using GameGuild.CQRS;
using GameGuild.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Resources.IntegrationTests;

/// <summary>
/// Integration tests for resource quota functionality with concurrency scenarios
/// </summary>
public class ResourceQuotaIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IResourceQuotaRepository _repository;
    private readonly IResourceQuotaService _service;
    private readonly Mock<IPublisher> _publisherMock;

    public ResourceQuotaIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ResourceQuotaRepository(_context);
        _publisherMock = new Mock<IPublisher>();
        var usageRepository = new UsageRecordRepository(_context);
        _service = new ResourceQuotaService(
            _repository,
            usageRepository,
            _publisherMock.Object,
            NullLogger<ResourceQuotaService>.Instance);
    }

    [Fact]
    public async Task ConcurrentCreates_DoNotExceedQuota_WithRaceCondition()
    {
        // Arrange: Create a quota with limit of 10
        var tenantId = Guid.NewGuid();
        await _service.SetQuotaAsync(tenantId, ResourceUsageType.Users, softLimit: 8, hardLimit: 10);

        // Simulate 20 concurrent create requests
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    var (success, _, _) = await _service.TryAtomicConsumeAsync(
                        tenantId,
                        ResourceUsageType.Users,
                        amount: 1);
                    return success;
                }
                catch
                {
                    return false;
                }
            }))
            .ToArray();

        // Act
        var results = await Task.WhenAll(tasks);
        var successCount = results.Count(r => r);

        // Assert
        successCount.Should().BeLessOrEqualTo(10, "quota enforcement should prevent more than 10 successful increments");
        
        var quota = await _service.GetQuotaAsync(tenantId, ResourceUsageType.Users);
        quota.Should().NotBeNull();
        quota!.CurrentUsage.Should().BeLessOrEqualTo(10, "final usage must not exceed hard limit");
        quota.CurrentUsage.Should().Be(successCount, "usage should match number of successful operations");
    }

    [Fact]
    public async Task ConcurrentCreates_WithExactQuotaRemaining_OnlyOneSucceeds()
    {
        // Arrange: Tenant with quota 10, current usage 9
        var tenantId = Guid.NewGuid();
        await _service.SetQuotaAsync(tenantId, ResourceUsageType.Users, softLimit: 8, hardLimit: 10);
        
        // Pre-consume 9 slots
        for (int i = 0; i < 9; i++)
        {
            await _service.TryAtomicConsumeAsync(tenantId, ResourceUsageType.Users, 1);
        }

        // Act: Fire 10 concurrent create requests (only 1 slot remaining)
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    var (success, _, _) = await _service.TryAtomicConsumeAsync(
                        tenantId,
                        ResourceUsageType.Users,
                        amount: 1);
                    return success;
                }
                catch
                {
                    return false;
                }
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert: Exactly 1 should succeed
        results.Count(r => r).Should().Be(1, "only one request should get the last remaining slot");

        // Assert: Quota should be exactly at limit
        var quota = await _service.GetQuotaAsync(tenantId, ResourceUsageType.Users);
        quota.Should().NotBeNull();
        quota!.CurrentUsage.Should().Be(10, "usage should be exactly at the hard limit");
    }

    [Fact]
    public async Task CreateAndDelete_MaintainsAccurateQuota_OverMultipleOperations()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await _service.SetQuotaAsync(tenantId, ResourceUsageType.Users, softLimit: 50, hardLimit: 100);

        // Act: Perform mixed create and delete operations
        for (int cycle = 0; cycle < 5; cycle++)
        {
            // Create 10 users
            for (int i = 0; i < 10; i++)
            {
                var (success, _, _) = await _service.TryAtomicConsumeAsync(
                    tenantId,
                    ResourceUsageType.Users,
                    amount: 1);
                success.Should().BeTrue();
            }

            // Verify count
            var quotaAfterCreates = await _service.GetQuotaAsync(tenantId, ResourceUsageType.Users);
            quotaAfterCreates!.CurrentUsage.Should().Be((cycle + 1) * 10);

            // Delete 5 users
            for (int i = 0; i < 5; i++)
            {
                var decremented = await _repository.DecrementUsageAsync(
                    tenantId,
                    ResourceUsageType.Users,
                    amount: 1);
                decremented.Should().BeTrue();
            }

            // Verify count after deletes
            var quotaAfterDeletes = await _service.GetQuotaAsync(tenantId, ResourceUsageType.Users);
            quotaAfterDeletes!.CurrentUsage.Should().Be((cycle + 1) * 10 - 5);
        }

        // Final assertion: 50 creates - 25 deletes = 25
        var finalQuota = await _service.GetQuotaAsync(tenantId, ResourceUsageType.Users);
        finalQuota!.CurrentUsage.Should().Be(25, "final usage should be accurate after multiple create/delete cycles");
    }

    [Fact]
    public async Task RollbackOnFailure_DoesNotIncrementQuota()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await _service.SetQuotaAsync(tenantId, ResourceUsageType.Users, softLimit: null, hardLimit: 10);

        var initialQuota = await _service.GetQuotaAsync(tenantId, ResourceUsageType.Users);
        var initialUsage = initialQuota!.CurrentUsage;

        // Act: Simulate a transaction that would increment quota but then fail
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Try to consume quota
            var (success, currentUsage, hardLimit) = await _service.TryAtomicConsumeAsync(
                tenantId,
                ResourceUsageType.Users,
                amount: 1);
            
            success.Should().BeTrue();
            
            // Simulate failure - rollback
            await transaction.RollbackAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
        }

        // Assert: Quota should not have been incremented
        _context.ChangeTracker.Clear(); // Clear tracking to force fresh read
        var quotaAfterRollback = await _service.GetQuotaAsync(tenantId, ResourceUsageType.Users);
        quotaAfterRollback!.CurrentUsage.Should().Be(initialUsage, "quota should not change after rollback");
    }

    [Fact]
    public async Task QuotaReset_HandledCorrectly_UnderConcurrency()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var quota = await _service.SetQuotaAsync(
            tenantId,
            ResourceUsageType.ApiCalls,
            softLimit: 800,
            hardLimit: 1000,
            period: ResourceQuotaPeriod.Daily);

        // Pre-fill quota to near limit
        for (int i = 0; i < 900; i++)
        {
            await _service.TryAtomicConsumeAsync(tenantId, ResourceUsageType.ApiCalls, 1);
        }

        // Act: Manually reset quota and immediately fire concurrent requests
        quota.CurrentUsage = 0;
        quota.LastReset = DateTime.UtcNow;
        await _repository.UpdateAsync(quota);

        var tasks = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(async () =>
            {
                var (success, _, _) = await _service.TryAtomicConsumeAsync(
                    tenantId,
                    ResourceUsageType.ApiCalls,
                    amount: 1);
                return success;
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert: All 50 should succeed since we reset to 0
        results.Count(r => r).Should().Be(50, "all requests should succeed after quota reset");
        
        var finalQuota = await _service.GetQuotaAsync(tenantId, ResourceUsageType.ApiCalls);
        finalQuota!.CurrentUsage.Should().Be(50, "usage should match successful operations after reset");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
