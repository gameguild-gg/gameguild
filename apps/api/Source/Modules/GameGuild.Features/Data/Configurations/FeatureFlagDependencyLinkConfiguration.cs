using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Features;

/// <summary>
///     Entity Type Configuration for FeatureFlagDependencyLink.
/// </summary>
public sealed class FeatureFlagDependencyLinkConfiguration : IEntityTypeConfiguration<FeatureFlagDependencyLink>
{
    public void Configure(EntityTypeBuilder<FeatureFlagDependencyLink> builder)
    {
        builder.ToTable("feature_flag_dependencies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FeatureFlagId)
            .IsRequired()
            .HasColumnName("feature_flag_id");

        builder.Property(x => x.DependsOnFeatureFlagId)
            .IsRequired()
            .HasColumnName("depends_on_feature_flag_id");

        builder.Property(x => x.DependencyType)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("dependency_type");

        builder.HasOne(x => x.FeatureFlag)
            .WithMany()
            .HasForeignKey(x => x.FeatureFlagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DependsOnFeatureFlag)
            .WithMany()
            .HasForeignKey(x => x.DependsOnFeatureFlagId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.FeatureFlagId)
            .HasDatabaseName("idx_feature_flag_dependencies_feature_flag_id");

        builder.HasIndex(x => x.DependsOnFeatureFlagId)
            .HasDatabaseName("idx_feature_flag_dependencies_depends_on_feature_flag_id");

        builder.HasIndex(x => new { x.FeatureFlagId, x.DependsOnFeatureFlagId, x.DependencyType })
            .IsUnique()
            .HasDatabaseName("idx_feature_flag_dependencies_unique_edge");
    }
}
