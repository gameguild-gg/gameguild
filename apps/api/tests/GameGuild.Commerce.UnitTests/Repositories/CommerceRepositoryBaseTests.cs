using FluentAssertions;
using GameGuild;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace GameGuild.Commerce.UnitTests.Repositories;

public class CommerceRepositoryBaseTests
{
    [Fact]
    public async Task CreateAsync_Should_Add_Entity()
    {
        await using var context = CreateContext();
        var repository = new PricingRuleRepository(context);
        var entity = new PricingRule { Name = "Rule", RuleType = PricingRuleType.Percentage, DiscountPercentage = 10 };

        var result = await repository.CreateAsync(entity);

        result.Id.Should().NotBeEmpty();
        (await context.PricingRules.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_For_SoftDeleted_Entity()
    {
        await using var context = CreateContext();
        var repository = new PricingRuleRepository(context);
        var entity = new PricingRule { Name = "Rule", RuleType = PricingRuleType.FixedAmount, DiscountAmount = 5 };

        context.PricingRules.Add(entity);
        await context.SaveChangesAsync();

        entity.SoftDelete();
        await context.SaveChangesAsync();

        var result = await repository.GetByIdAsync(entity.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_Should_Filter_By_Predicate()
    {
        await using var context = CreateContext();
        var repository = new PricingRuleRepository(context);
        context.PricingRules.AddRange(
            new PricingRule { Name = "A", RuleType = PricingRuleType.Percentage, DiscountPercentage = 5 },
            new PricingRule { Name = "B", RuleType = PricingRuleType.FixedAmount, DiscountAmount = 10 }
        );
        await context.SaveChangesAsync();

        var results = await repository.GetAllAsync(r => r.RuleType == PricingRuleType.Percentage);

        results.Should().HaveCount(1);
        results.First().Name.Should().Be("A");
    }

    [Fact]
    public async Task GetPagedAsync_Should_Order_By_CreatedAt_Desc()
    {
        await using var context = CreateContext();
        var repository = new PricingRuleRepository(context);

        var first = new PricingRule { Name = "Old", RuleType = PricingRuleType.Percentage, DiscountPercentage = 5 };
        var second = new PricingRule { Name = "New", RuleType = PricingRuleType.FixedAmount, DiscountAmount = 10 };

        typeof(EntityBase).GetProperty(nameof(EntityBase.CreatedAt))!.SetValue(first, DateTime.UtcNow.AddMinutes(-10));
        typeof(EntityBase).GetProperty(nameof(EntityBase.CreatedAt))!.SetValue(second, DateTime.UtcNow.AddMinutes(-1));

        context.PricingRules.AddRange(first, second);
        await context.SaveChangesAsync();

        var results = (await repository.GetPagedAsync(0, 1)).ToList();

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("New");
    }

    [Fact]
    public async Task GetPagedAsync_Should_Filter_With_Predicate()
    {
        await using var context = CreateContext();
        var repository = new PricingRuleRepository(context);

        context.PricingRules.AddRange(
            new PricingRule { Name = "A", RuleType = PricingRuleType.Percentage, DiscountPercentage = 5 },
            new PricingRule { Name = "B", RuleType = PricingRuleType.FixedAmount, DiscountAmount = 10 }
        );
        await context.SaveChangesAsync();

        var results = await repository.GetPagedAsync(0, 10, r => r.RuleType == PricingRuleType.FixedAmount);

        results.Should().HaveCount(1);
        results.First().Name.Should().Be("B");
    }

    [Fact]
    public async Task CountAsync_Should_Return_Matching_Count()
    {
        await using var context = CreateContext();
        var repository = new PricingRuleRepository(context);
        context.PricingRules.AddRange(
            new PricingRule { Name = "A", RuleType = PricingRuleType.Percentage, DiscountPercentage = 5 },
            new PricingRule { Name = "B", RuleType = PricingRuleType.FixedAmount, DiscountAmount = 10 }
        );
        await context.SaveChangesAsync();

        var count = await repository.CountAsync(r => r.RuleType == PricingRuleType.FixedAmount);

        count.Should().Be(1);
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_True_When_Entity_Matches()
    {
        await using var context = CreateContext();
        var repository = new PricingRuleRepository(context);
        context.PricingRules.Add(new PricingRule { Name = "A", RuleType = PricingRuleType.Percentage, DiscountPercentage = 5 });
        await context.SaveChangesAsync();

        var exists = await repository.ExistsAsync(r => r.RuleType == PricingRuleType.Percentage);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_Should_Touch_Entity()
    {
        await using var context = CreateContext();
        var repository = new PricingRuleRepository(context);
        var entity = new PricingRule { Name = "Rule", RuleType = PricingRuleType.FixedAmount, DiscountAmount = 5 };
        context.PricingRules.Add(entity);
        await context.SaveChangesAsync();

        var originalUpdatedAt = entity.UpdatedAt;
        entity.Name = "Updated";

        var result = await repository.UpdateAsync(entity);

        result.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_Should_SoftDelete_Entity()
    {
        await using var context = CreateContext();
        var repository = new PricingRuleRepository(context);
        var entity = new PricingRule { Name = "Rule", RuleType = PricingRuleType.Percentage, DiscountPercentage = 5 };
        context.PricingRules.Add(entity);
        await context.SaveChangesAsync();

        var deleted = await repository.DeleteAsync(entity.Id);

        deleted.Should().BeTrue();
        entity.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_Should_ReturnFalse_When_NotFound()
    {
        await using var context = CreateContext();
        var repository = new PricingRuleRepository(context);

        var deleted = await repository.DeleteAsync(Guid.NewGuid());

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task HardDeleteAsync_Should_Remove_Entity()
    {
        await using var context = CreateContext();
        var repository = new PricingRuleRepository(context);
        var entity = new PricingRule { Name = "Rule", RuleType = PricingRuleType.Percentage, DiscountPercentage = 5 };
        context.PricingRules.Add(entity);
        await context.SaveChangesAsync();

        var deleted = await repository.HardDeleteAsync(entity.Id);

        deleted.Should().BeTrue();
        (await context.PricingRules.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task HardDeleteAsync_Should_ReturnFalse_When_NotFound()
    {
        await using var context = CreateContext();
        var repository = new PricingRuleRepository(context);

        var deleted = await repository.HardDeleteAsync(Guid.NewGuid());

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetByTenantAsync_Should_Filter_By_TenantId()
    {
        await using var context = CreateContext();
        var repository = new PricingRuleRepository(context);
        var tenantId = Guid.NewGuid();

        var inTenant = new PricingRule { Name = "TenantRule", RuleType = PricingRuleType.Percentage, DiscountPercentage = 5 };
        var other = new PricingRule { Name = "Other", RuleType = PricingRuleType.FixedAmount, DiscountAmount = 10 };

        SetTenantId(inTenant, tenantId);
        SetTenantId(other, Guid.NewGuid());

        context.PricingRules.AddRange(inTenant, other);
        await context.SaveChangesAsync();

        var results = await repository.GetByTenantAsync(tenantId);

        results.Should().HaveCount(1);
        results.First().Name.Should().Be("TenantRule");
    }

    private static void SetTenantId(PricingRule entity, Guid tenantId)
    {
        var property = typeof(PricingRule).GetProperty("TenantId");
        property!.SetValue(entity, tenantId);
    }

    private static TestCommerceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestCommerceDbContext>()
            .UseInMemoryDatabase($"Commerce_{Guid.NewGuid()}")
            .Options;

        return new TestCommerceDbContext(options);
    }

    private sealed class TestCommerceDbContext(DbContextOptions<TestCommerceDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<PricingRule> PricingRules { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<PricingRule>(builder =>
            {
                builder.HasKey(r => r.Id);
                builder.Property(r => r.Name).IsRequired();
            });
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Mock.Of<IDbContextTransaction>());
        }
    }

    private sealed class PricingRuleRepository(TestCommerceDbContext context)
        : CommerceRepositoryBase<PricingRule>(context)
    {
    }
}
