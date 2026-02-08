using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     EF Core model configuration for the Subscriptions module.
/// </summary>
public sealed class SubscriptionsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(Subscription).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Commerce.Subscriptions", StringComparison.Ordinal) == true);
    }
}
