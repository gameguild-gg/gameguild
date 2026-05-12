using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
/// Read model for subscription invoice history backed by the shared invoices table.
/// This avoids a direct project reference from Subscriptions to Commerce.Billing.
/// </summary>
public sealed class SubscriptionInvoiceReadModel
{
    public Guid Id { get; init; }

    public Guid SubscriptionId { get; init; }

    public string InvoiceNumber { get; init; } = string.Empty;

    public decimal Total { get; init; }

    public string Currency { get; init; } = "USD";

    public DateTime CreatedAt { get; init; }

    public DateTime? IssuedAt { get; init; }

    public DateTime? DueDate { get; init; }

    public DateTime? PaidAt { get; init; }

    public int Status { get; init; }

    public Guid? PaymentId { get; init; }

    public string? ExternalId { get; init; }
}

public sealed class SubscriptionInvoiceReadModelConfiguration : IEntityTypeConfiguration<SubscriptionInvoiceReadModel>
{
    public void Configure(EntityTypeBuilder<SubscriptionInvoiceReadModel> builder)
    {
        builder.HasNoKey();
        builder.ToSqlQuery(
            """
             SELECT \"Id\",
                 \"SubscriptionId\",
                 \"InvoiceNumber\",
                 \"Total\",
                 \"Currency\",
                 \"CreatedAt\",
                 \"IssuedAt\",
                 \"DueDate\",
                 \"PaidAt\",
                 \"Status\",
                 \"PaymentId\",
                 \"ExternalId\"
            FROM invoices
            """);

        builder.Property(x => x.InvoiceNumber).HasMaxLength(50);
        builder.Property(x => x.Total).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.ExternalId).HasMaxLength(255);
    }
}
