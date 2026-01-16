using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Entity Type Configuration for TaxRate
/// </summary>
public class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>
{
    public void Configure(EntityTypeBuilder<TaxRate> builder)
    {
        // Configure table name
        builder.ToTable("tax_rates", tb =>
        {
            tb.HasCheckConstraint("CK_TaxRate_Rate_Valid", "\"Rate\" >= 0 AND \"Rate\" <= 1");
        });

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Property configurations
        builder.Property(x => x.TaxJurisdictionId)
            .IsRequired();

        builder.Property(x => x.TaxType)
            .IsRequired();

        builder.Property(x => x.Rate)
            .HasColumnType("decimal(5,4)")
            .IsRequired();

        builder.Property(x => x.ProductCategory)
            .HasMaxLength(100);

        builder.Property(x => x.EffectiveFrom)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.MinimumTaxableAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.MaximumTaxableAmount)
            .HasColumnType("decimal(18,2)");

        // Relationship configurations
        builder.HasOne(x => x.TaxJurisdiction)
            .WithMany()
            .HasForeignKey(x => x.TaxJurisdictionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes for performance
        builder.HasIndex(x => x.TaxJurisdictionId).HasDatabaseName("ix_tax_rates_jurisdiction_id");
        builder.HasIndex(x => x.TaxType).HasDatabaseName("ix_tax_rates_tax_type");
        builder.HasIndex(x => x.IsActive).HasDatabaseName("ix_tax_rates_is_active");
        builder.HasIndex(x => x.EffectiveFrom).HasDatabaseName("ix_tax_rates_effective_from");
        builder.HasIndex(x => x.EffectiveTo).HasDatabaseName("ix_tax_rates_effective_to");
    }
}
