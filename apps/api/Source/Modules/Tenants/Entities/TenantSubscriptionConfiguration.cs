namespace GameGuild.Modules.Tenants;

/// <summary>
///     Entity Framework configuration for the TenantSubscription entity
/// </summary>
public class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
{
    public void Configure(EntityTypeBuilder<TenantSubscription> builder)
    {
        // Table configuration
        builder.ToTable("tenant_subscriptions");

        // Primary key
        builder.HasKey(ts => ts.Id);

        // Properties
        builder.Property(ts => ts.TenantId)
            .IsRequired();

        builder.Property(ts => ts.PlanId)
            .IsRequired();

        builder.Property(ts => ts.PlanName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ts => ts.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Active");

        builder.Property(ts => ts.StartsAt)
            .IsRequired();

        builder.Property(ts => ts.AutoRenew)
            .HasDefaultValue(true);

        builder.Property(ts => ts.BillingInterval)
            .HasMaxLength(50);

        builder.Property(ts => ts.Cost)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0);

        builder.Property(ts => ts.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        builder.Property(ts => ts.PaymentProviderId)
            .HasMaxLength(255);

        builder.Property(ts => ts.Metadata)
            .HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(ts => ts.TenantId)
            .IsUnique()
            .HasDatabaseName("ix_tenant_subscriptions_tenant");

        builder.HasIndex(ts => ts.PlanId)
            .HasDatabaseName("ix_tenant_subscriptions_plan");

        builder.HasIndex(ts => ts.Status)
            .HasDatabaseName("ix_tenant_subscriptions_status");

        builder.HasIndex(ts => ts.ExpiresAt)
            .HasDatabaseName("ix_tenant_subscriptions_expires_at");

        // Relationships
        builder.HasOne(ts => ts.Tenant)
            .WithMany()
            .HasForeignKey(ts => ts.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete query filter
        builder.HasQueryFilter(ts => !ts.IsDeleted);
    }
}
