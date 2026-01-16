using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Entity Type Configuration for PricingRule
/// </summary>
public class PricingRuleConfiguration : IEntityTypeConfiguration<PricingRule>
{
    public void Configure(EntityTypeBuilder<PricingRule> builder)
    {
        // Configure table name
        builder.ToTable("pricing_rules");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Property configurations
        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.RuleType)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        // Configure relationships
        builder.HasMany(x => x.PricingTiers)
            .WithOne(t => t.PricingRule)
            .HasForeignKey(t => t.PricingRuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
