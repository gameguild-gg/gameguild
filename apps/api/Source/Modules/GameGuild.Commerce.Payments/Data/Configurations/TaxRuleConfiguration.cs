using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Entity Type Configuration for TaxRule
/// </summary>
public class TaxRuleConfiguration : IEntityTypeConfiguration<TaxRule>
{
    public void Configure(EntityTypeBuilder<TaxRule> builder)
    {
        // Configure table name
        builder.ToTable("tax_rules");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Property configurations
        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.TaxJurisdictionId)
            .IsRequired();

        builder.Property(x => x.RuleType)
            .IsRequired();

        builder.Property(x => x.Priority)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        // Relationship configurations
        builder.HasOne(x => x.TaxJurisdiction)
            .WithMany(j => j.TaxRules)
            .HasForeignKey(x => x.TaxJurisdictionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes for performance
        builder.HasIndex(x => x.TaxJurisdictionId).HasDatabaseName("ix_tax_rules_jurisdiction_id");
        builder.HasIndex(x => x.RuleType).HasDatabaseName("ix_tax_rules_rule_type");
        builder.HasIndex(x => x.IsActive).HasDatabaseName("ix_tax_rules_is_active");
        builder.HasIndex(x => x.Priority).HasDatabaseName("ix_tax_rules_priority");
        builder.HasIndex(x => x.EffectiveFrom).HasDatabaseName("ix_tax_rules_effective_from");
        builder.HasIndex(x => x.EffectiveTo).HasDatabaseName("ix_tax_rules_effective_to");
    }
}
