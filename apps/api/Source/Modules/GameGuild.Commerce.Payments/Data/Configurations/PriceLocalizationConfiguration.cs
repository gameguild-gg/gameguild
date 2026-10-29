using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Entity Type Configuration for <see cref="PriceLocalization"/> (issue #412 spec C).
/// </summary>
public class PriceLocalizationConfiguration : IEntityTypeConfiguration<PriceLocalization>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PriceLocalization> builder)
    {
        builder.ToTable("price_localizations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.LocalizedPrice)
            .HasColumnType("decimal(28,12)");

        builder.Property(x => x.ExchangeRateUsed)
            .HasColumnType("decimal(28,12)");

        // One row per tenant + plan + currency + effective date: future-dated price
        // changes coexist with the currently effective row.
        builder.HasIndex(x => new { x.TenantId, x.PlanId, x.CurrencyCode, x.EffectiveDateUtc })
            .IsUnique()
            .HasDatabaseName("ix_price_localizations_tenant_plan_currency_effective");
    }
}

/// <summary>
///     Entity Type Configuration for <see cref="PriceRecalculationRun"/> (issue #412 spec C).
/// </summary>
public class PriceRecalculationRunConfiguration : IEntityTypeConfiguration<PriceRecalculationRun>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PriceRecalculationRun> builder)
    {
        builder.ToTable("price_recalculation_runs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.TriggerReason)
            .HasMaxLength(200);

        // The before-snapshot is audit data consumed by the rollback command; cap it well
        // below the PostgreSQL text limit while allowing thousands of rewritten rows.
        builder.Property(x => x.BeforeSnapshot)
            .HasMaxLength(1_000_000);
    }
}
