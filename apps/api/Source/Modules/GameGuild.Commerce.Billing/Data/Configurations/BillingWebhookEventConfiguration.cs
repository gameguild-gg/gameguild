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

        builder.Property(x => x.ProviderEnvironment)
            .HasMaxLength(32);

        builder.Property(x => x.ProviderAccountId)
            .HasMaxLength(255);

        builder.Property(x => x.WebhookEndpointId)
            .HasMaxLength(255);

        builder.Property(x => x.ProviderObjectId)
            .HasMaxLength(255);

        builder.Property(x => x.ProviderObjectType)
            .HasMaxLength(100);

        builder.Property(x => x.ProviderMonetaryLeg)
            .HasMaxLength(100);

        builder.Property(x => x.IsLiveMode);

        builder.Property(x => x.EventSchemaVersion)
            .HasMaxLength(50);

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

        builder.HasIndex(x => new
            {
                x.Provider,
                x.ProviderEnvironment,
                x.ProviderAccountId,
                x.WebhookEndpointId,
                x.ExternalEventId
            })
            .HasFilter("\"ProviderEnvironment\" IS NOT NULL AND \"ProviderAccountId\" IS NOT NULL AND \"WebhookEndpointId\" IS NOT NULL")
            .IsCreatedConcurrently()
            .HasDatabaseName("ix_billing_webhook_events_provider_scope_event");

        builder.HasIndex(x => new
            {
                x.Provider,
                x.ProviderEnvironment,
                x.ProviderAccountId,
                x.ProviderObjectId,
                x.ProviderMonetaryLeg
            })
            .HasFilter("\"ProviderEnvironment\" IS NOT NULL AND \"ProviderAccountId\" IS NOT NULL AND \"ProviderObjectId\" IS NOT NULL AND \"ProviderMonetaryLeg\" IS NOT NULL")
            .IsCreatedConcurrently()
            .HasDatabaseName("ix_billing_webhook_events_provider_object_leg");
        
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
