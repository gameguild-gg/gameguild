using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Entity Type Configuration for PromoStackingRule
/// </summary>
public class PromoStackingRuleConfiguration : IEntityTypeConfiguration<PromoStackingRule>
{
    public void Configure(EntityTypeBuilder<PromoStackingRule> builder)
    {
        // Configure table name
        builder.ToTable("promo_stacking_rules");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Property configurations
        builder.Property(x => x.PromoCodeId)
            .IsRequired();

        builder.Property(x => x.StackBehavior)
            .IsRequired();

        builder.Property(x => x.AllowedPromoCodeIds)
            .HasMaxLength(2000);

        builder.Property(x => x.ExcludedPromoCodeIds)
            .HasMaxLength(2000);

        builder.Property(x => x.PromoCodeTypes)
            .HasMaxLength(1000);

        builder.Property(x => x.Priority)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        // Configure indexes for performance
        builder.HasIndex(x => x.PromoCodeId).HasDatabaseName("ix_promo_stacking_rules_promo_code_id");
        builder.HasIndex(x => x.StackBehavior).HasDatabaseName("ix_promo_stacking_rules_stack_behavior");
        builder.HasIndex(x => x.Priority).HasDatabaseName("ix_promo_stacking_rules_priority");
    }
}
