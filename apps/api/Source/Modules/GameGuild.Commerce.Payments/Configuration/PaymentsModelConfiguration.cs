using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     EF Core model configuration for the Payments module (tax-related entities only).
///     Additional payment entities are currently disabled.
/// </summary>
public sealed class PaymentsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>();
        modelBuilder.ApplyConfiguration(new TaxJurisdictionConfiguration());
        modelBuilder.ApplyConfiguration(new TaxRateConfiguration());
        modelBuilder.ApplyConfiguration(new TaxRuleConfiguration());
        modelBuilder.ApplyConfiguration(new UserWalletConfiguration());
    }
}
