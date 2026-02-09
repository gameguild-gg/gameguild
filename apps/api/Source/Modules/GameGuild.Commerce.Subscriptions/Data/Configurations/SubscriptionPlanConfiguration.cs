using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Entity Type Configuration for SubscriptionPlan
/// </summary>
public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        // Configure table
        builder.ToTable("subscription_plans");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Property configurations
        builder.Property(x => x.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.ExternalId)
            .HasMaxLength(100);

        builder.Property(x => x.Features)
            .HasMaxLength(2000);

        builder.Property(x => x.Metadata)
            .HasMaxLength(4000);

        // Relationship configurations
        builder.HasMany(x => x.Subscriptions)
            .WithOne(s => s.Plan)
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure indexes for performance
        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("ix_subscription_plans_name");
        builder.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("ix_subscription_plans_slug");
        builder.HasIndex(x => x.ExternalId).IsUnique().HasDatabaseName("ix_subscription_plans_external_id");
        builder.HasIndex(x => x.IsActive).HasDatabaseName("ix_subscription_plans_is_active");
        builder.HasIndex(x => x.IsFeatured).HasDatabaseName("ix_subscription_plans_is_featured");
        builder.HasIndex(x => x.SortOrder).HasDatabaseName("ix_subscription_plans_sort_order");
    }
}
