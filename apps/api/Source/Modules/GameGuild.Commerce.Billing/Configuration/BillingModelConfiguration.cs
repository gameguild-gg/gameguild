using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     EF Core model configuration for the Commerce.Billing module.
/// </summary>
public sealed class BillingModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(BillingWebhookEvent).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Commerce.Billing", StringComparison.Ordinal) == true);

        // Invoice relies on data annotations for mapping, so it still needs explicit model registration.
        modelBuilder.Entity<Invoice>();
    }
}
