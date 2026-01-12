using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Entity Type Configuration for PolicyDefinitionEntity.
/// </summary>
public class PolicyDefinitionEntityConfiguration : IEntityTypeConfiguration<PolicyDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<PolicyDefinitionEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).IsRequired();

        builder.Property(x => x.PolicyName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.AuthenticationSchemesJson)
            .HasMaxLength(1000);

        builder.Property(x => x.RequiredPermissionsJson)
            .HasMaxLength(2000);

        builder.Property(x => x.RequiredRolesJson)
            .HasMaxLength(1000);

        builder.Property(x => x.ResourceType)
            .HasMaxLength(100);

        builder.Property(x => x.MinimumAccessLevel)
            .HasMaxLength(50);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        // Rule-based evaluation columns
        builder.Property(x => x.RulesJson);

        builder.Property(x => x.UseRuleBasedEvaluation);
    }
}
