using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Entity Type Configuration for PricingTier
/// </summary>
public class PricingTierConfiguration : IEntityTypeConfiguration<PricingTier>
{
    public void Configure(EntityTypeBuilder<PricingTier> builder)
    {
        // Configure table name with constraints
        builder.ToTable("pricing_tiers", tb =>
        {
            tb.HasCheckConstraint("CK_PricingTier_Quantity_Valid", "min_quantity IS NULL OR max_quantity IS NULL OR min_quantity <= max_quantity");
            tb.HasCheckConstraint("CK_PricingTier_Price_NonNegative", "price IS NULL OR price >= 0");
            tb.HasCheckConstraint("CK_PricingTier_Discount_Valid", "discount_percentage IS NULL OR (discount_percentage >= 0 AND discount_percentage <= 100)");
        });

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Property configurations
        builder.Property(x => x.PricingRuleId)
            .IsRequired();

        builder.Property(x => x.MinQuantity)
            .HasColumnName("min_quantity");

        builder.Property(x => x.MaxQuantity)
            .HasColumnName("max_quantity");

        builder.Property(x => x.Price)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.DiscountPercentage)
            .HasColumnName("discount_percentage")
            .HasColumnType("decimal(5,2)");

        // Relationship configurations
        builder.HasOne(x => x.PricingRule)
            .WithMany()
            .HasForeignKey(x => x.PricingRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes for performance
        builder.HasIndex(x => x.PricingRuleId).HasDatabaseName("ix_pricing_tiers_pricing_rule_id");
        builder.HasIndex(x => x.MinQuantity).HasDatabaseName("ix_pricing_tiers_min_quantity");
        builder.HasIndex(x => x.MaxQuantity).HasDatabaseName("ix_pricing_tiers_max_quantity");
    }
}
