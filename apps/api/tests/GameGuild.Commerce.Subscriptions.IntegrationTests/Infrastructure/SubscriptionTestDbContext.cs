using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions.IntegrationTests.Infrastructure;

/// <summary>
/// Test-specific DbContext that includes Subscription module entities
/// which are not enabled in the production ApplicationDbContext
/// </summary>
public class SubscriptionTestDbContext : ApplicationDbContext
{
    public SubscriptionTestDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Subscriptions DbSet for testing
    /// </summary>
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    /// <summary>
    /// Subscription Plans DbSet for testing
    /// </summary>
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply Subscriptions module configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Subscription).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Commerce.Subscriptions.Data.Configurations") == true);
    }
}
