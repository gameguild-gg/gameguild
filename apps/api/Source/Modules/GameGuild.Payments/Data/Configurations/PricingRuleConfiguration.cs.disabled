using GameGuild.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Payments.Data.Configurations;

/// <summary>
///     Entity Type Configuration for PricingRule
///     NOTE: PricingRule is abstract, so this configuration is disabled.
///     Derived concrete classes should have their own configurations.
/// </summary>
public class PricingRuleConfiguration : IEntityTypeConfiguration<PricingRule>
{
    public void Configure(EntityTypeBuilder<PricingRule> builder)
    {
        // Ignore the abstract base class - only concrete derived types should be mapped
        builder.Ignore();
    }
}
