using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Features;

public sealed class FeatureFlagDependencyLinkConfiguration : IEntityTypeConfiguration<FeatureFlagDependencyLink>
{
    public void Configure(EntityTypeBuilder<FeatureFlagDependencyLink> builder)
    {
        builder.ToTable("feature_flag_dependencies");
        builder.HasKey(link => link.Id);
        builder.Property(link => link.FeatureFlagId).HasColumnName("feature_flag_id").IsRequired();
        builder.Property(link => link.DependsOnFeatureFlagId).HasColumnName("depends_on_feature_flag_id").IsRequired();
        builder.Property(link => link.DependencyType).HasColumnName("dependency_type").HasMaxLength(50).IsRequired();
        builder.HasOne(link => link.FeatureFlag)
            .WithMany()
            .HasForeignKey(link => link.FeatureFlagId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(link => link.DependsOnFeatureFlag)
            .WithMany()
            .HasForeignKey(link => link.DependsOnFeatureFlagId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(link => link.FeatureFlagId)
            .HasDatabaseName("idx_feature_flag_dependencies_feature_flag_id");
        builder.HasIndex(link => link.DependsOnFeatureFlagId)
            .HasDatabaseName("idx_feature_flag_dependencies_depends_on_feature_flag_id");
        builder.HasIndex(link => new { link.FeatureFlagId, link.DependsOnFeatureFlagId, link.DependencyType })
            .IsUnique()
            .HasDatabaseName("idx_feature_flag_dependencies_unique_edge");
    }
}
