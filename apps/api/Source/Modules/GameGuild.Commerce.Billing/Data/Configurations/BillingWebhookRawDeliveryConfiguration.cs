using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Entity Type Configuration for BillingWebhookRawDelivery (issue #408: capture raw
///     payload and all headers — opt-in redacted raw-delivery retention).
/// </summary>
public class BillingWebhookRawDeliveryConfiguration : IEntityTypeConfiguration<BillingWebhookRawDelivery>
{
    public void Configure(EntityTypeBuilder<BillingWebhookRawDelivery> builder)
    {
        // Configure table (pluralized like the inbox table, kept in the same schema).
        builder.ToTable("billing_webhook_raw_deliveries");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // One redacted raw delivery per durable inbox row; purging the inbox row
        // cascades to its raw evidence.
        builder.HasIndex(x => x.BillingWebhookEventId)
            .IsUnique()
            .HasDatabaseName("ix_billing_webhook_raw_deliveries_event_id");

        builder.HasOne<BillingWebhookEvent>()
            .WithMany()
            .HasForeignKey(x => x.BillingWebhookEventId)
            .HasConstraintName("FK_billing_webhook_raw_deliveries_event")
            .OnDelete(DeleteBehavior.Cascade);

        // Property configurations
        builder.Property(x => x.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ExternalEventId)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.RawPayload)
            .IsRequired();

        builder.Property(x => x.RawHeaders)
            .IsRequired();

        builder.Property(x => x.PayloadSha256)
            .HasMaxLength(64)
            .IsRequired();

        // TTL cleanup scan (issue #408): the retention worker purges rows whose
        // deadline has elapsed.
        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("ix_billing_webhook_raw_deliveries_expires_at");

        builder.HasIndex(x => x.ReceivedAt)
            .HasDatabaseName("ix_billing_webhook_raw_deliveries_received_at");
    }
}
