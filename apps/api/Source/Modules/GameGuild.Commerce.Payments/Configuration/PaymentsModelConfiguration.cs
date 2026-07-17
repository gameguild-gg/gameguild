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
        modelBuilder.Entity<Payment>(builder =>
        {
            builder.Property(payment => payment.ProviderEnvironment)
                .HasMaxLength(32);
            builder.Property(payment => payment.ProviderAccountId)
                .HasMaxLength(255);
            builder.Property(payment => payment.ProviderObjectId)
                .HasMaxLength(255);
            builder.Property(payment => payment.ProviderObjectType)
                .HasMaxLength(100);
            builder.Property(payment => payment.ProviderMonetaryLeg)
                .HasMaxLength(100);

            builder.HasIndex(payment => new
                {
                    payment.Provider,
                    payment.ProviderEnvironment,
                    payment.ProviderAccountId,
                    payment.ProviderObjectId,
                    payment.ProviderMonetaryLeg
                })
                .HasFilter("\"ProviderEnvironment\" IS NOT NULL AND \"ProviderAccountId\" IS NOT NULL AND \"ProviderObjectId\" IS NOT NULL AND \"ProviderMonetaryLeg\" IS NOT NULL")
                .IsCreatedConcurrently()
                .HasDatabaseName("ix_payments_provider_object_leg");
        });
        modelBuilder.ApplyConfiguration(new TaxJurisdictionConfiguration());
        modelBuilder.ApplyConfiguration(new TaxRateConfiguration());
        modelBuilder.ApplyConfiguration(new TaxRuleConfiguration());
        modelBuilder.ApplyConfiguration(new UserWalletConfiguration());
    }
}
