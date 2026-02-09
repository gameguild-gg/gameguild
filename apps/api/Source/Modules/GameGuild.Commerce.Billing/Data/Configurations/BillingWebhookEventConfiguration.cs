using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Entity Type Configuration for BillingWebhookEvent
/// </summary>
public class BillingWebhookEventConfiguration : IEntityTypeConfiguration<BillingWebhookEvent>
{
    public void Configure(EntityTypeBuilder<BillingWebhookEvent> builder)
    {
        // Configure table
        builder.ToTable("billing_webhook_events");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Property configurations
        builder.Property(x => x.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ExternalEventId)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Payload)
            .IsRequired();

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        // Configure indexes for performance and idempotency
        builder.HasIndex(x => new { x.ExternalEventId, x.Provider })
            .IsUnique()
            .HasDatabaseName("ix_billing_webhook_events_external_id_provider");
        
        builder.HasIndex(x => x.EventType)
            .HasDatabaseName("ix_billing_webhook_events_event_type");
        
        builder.HasIndex(x => x.IsProcessed)
            .HasDatabaseName("ix_billing_webhook_events_is_processed");
        
        builder.HasIndex(x => x.IsFailed)
            .HasDatabaseName("ix_billing_webhook_events_is_failed");
        
        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_billing_webhook_events_tenant_id");
        
        builder.HasIndex(x => x.SubscriptionId)
            .HasDatabaseName("ix_billing_webhook_events_subscription_id");
        
        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("ix_billing_webhook_events_created_at");
    }
}
