namespace GameGuild.Modules.Billing.Models;

public class BillingWebhookEventConfiguration : IEntityTypeConfiguration<BillingWebhookEvent> {
  public void Configure(EntityTypeBuilder<BillingWebhookEvent> builder) {
    builder.ToTable("BillingWebhookEvents");

    builder.HasKey(e => e.Id);

    builder.Property(e => e.Provider).IsRequired().HasMaxLength(50);

    builder.Property(e => e.ExternalEventId).IsRequired().HasMaxLength(255);

    builder.Property(e => e.EventType).IsRequired().HasMaxLength(100);

    builder.Property(e => e.Payload).IsRequired();

    builder.Property(e => e.Headers);

    builder.Property(e => e.IsProcessed);

    builder.Property(e => e.IsFailed);

    builder.Property(e => e.ProcessingAttempts);

    builder.Property(e => e.ErrorMessage);

    builder.Property(e => e.ProcessedAt);

    builder.Property(e => e.TenantId);

    builder.Property(e => e.SubscriptionId);

    builder.Property(e => e.UserId);

    // Indexes for performance
    builder.HasIndex(e => new { e.Provider, e.ExternalEventId }).IsUnique().HasDatabaseName("IX_BillingWebhookEvents_Provider_ExternalEventId");

    builder.HasIndex(e => e.EventType).HasDatabaseName("IX_BillingWebhookEvents_EventType");

    builder.HasIndex(e => e.IsProcessed).HasDatabaseName("IX_BillingWebhookEvents_IsProcessed");

    builder.HasIndex(e => e.IsFailed).HasDatabaseName("IX_BillingWebhookEvents_IsFailed");

    builder.HasIndex(e => e.TenantId).HasDatabaseName("IX_BillingWebhookEvents_TenantId");

    builder.HasIndex(e => e.SubscriptionId).HasDatabaseName("IX_BillingWebhookEvents_SubscriptionId");

    builder.HasIndex(e => e.UserId).HasDatabaseName("IX_BillingWebhookEvents_UserId");

    builder.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_BillingWebhookEvents_CreatedAt");
  }
}
