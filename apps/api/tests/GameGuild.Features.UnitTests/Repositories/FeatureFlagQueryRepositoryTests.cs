using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using GameGuild.Features;
using Xunit;

namespace GameGuild.Features.UnitTests.Repositories;

public class FeatureFlagQueryRepositoryTests
{
    [Fact]
    public async Task GetTargetingRulesAsync_ReturnsActiveTargetsOrderedByPriority()
    {
        await using var db = CreateDbContext();
        var featureFlagId = Guid.NewGuid();
        var deletedTarget = new FeatureFlagTarget
        {
            FeatureFlagId = featureFlagId,
            TargetType = "tenant",
            TargetIdentifier = "deleted",
            Priority = 100
        };
        deletedTarget.DeletedAt = DateTime.UtcNow;

        db.Set<FeatureFlagTarget>().AddRange(
            new FeatureFlagTarget
            {
                FeatureFlagId = featureFlagId,
                TargetType = "tenant",
                TargetIdentifier = "low",
                IsEnabled = true,
                RolloutPercentage = 25,
                Priority = 1,
                CustomValue = "off"
            },
            new FeatureFlagTarget
            {
                FeatureFlagId = featureFlagId,
                TargetType = "tenant",
                TargetIdentifier = "high",
                IsEnabled = true,
                RolloutPercentage = 100,
                Priority = 10,
                CustomValue = "on",
                Metadata = "{\"source\":\"test\"}"
            },
            deletedTarget);
        await db.SaveChangesAsync();

        var repository = new FeatureFlagQueryRepository(db);

        var result = (await repository.GetTargetingRulesAsync(featureFlagId)).ToList();

        result.Should().HaveCount(2);
        result.Select(r => r.TargetIdentifier).Should().Equal("high", "low");
        result[0].CustomValue.Should().Be("on");
        result[0].Metadata.Should().Be("{\"source\":\"test\"}");
    }

    [Fact]
    public async Task GetTargetingRuleByIdAsync_ReturnsMappedActiveTarget()
    {
        await using var db = CreateDbContext();
        var target = new FeatureFlagTarget
        {
            FeatureFlagId = Guid.NewGuid(),
            TargetType = "plan",
            TargetIdentifier = "enterprise",
            IsEnabled = true,
            RolloutPercentage = 75,
            Priority = 3
        };
        db.Set<FeatureFlagTarget>().Add(target);
        await db.SaveChangesAsync();

        var repository = new FeatureFlagQueryRepository(db);

        var result = await repository.GetTargetingRuleByIdAsync(target.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(target.Id);
        result.TargetType.Should().Be("plan");
        result.TargetIdentifier.Should().Be("enterprise");
        result.RolloutPercentage.Should().Be(75);
    }

    private static FeatureFlagRepositoryTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FeatureFlagRepositoryTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FeatureFlagRepositoryTestDbContext(options);
    }

    private sealed class FeatureFlagRepositoryTestDbContext(DbContextOptions<FeatureFlagRepositoryTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FeatureFlagTarget>();
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Transactions are not needed for these repository tests.");
    }
}
