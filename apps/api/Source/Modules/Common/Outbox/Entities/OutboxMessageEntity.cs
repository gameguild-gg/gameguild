using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.Common.Outbox.Entities;

/// <summary>
/// Entity for storing outbox messages
/// </summary>
public sealed class OutboxMessageEntity
{
    public required Guid Id { get; init; }
    public required string MessageType { get; init; }
    public required string Payload { get; init; }
    public string? CorrelationId { get; init; }
    public required int Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public int RetryCount { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// EF Core configuration for OutboxMessageEntity
/// </summary>
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("outbox_messages", "common");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired();

        builder.Property(x => x.MessageType)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Payload)
            .IsRequired();

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(256);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ProcessedAt);

        builder.Property(x => x.RetryCount)
            .IsRequired();

        builder.Property(x => x.Error);

        builder.HasIndex(x => new { x.Status, x.CreatedAt })
            .HasDatabaseName("ix_outbox_messages_status_created_at");

        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("ix_outbox_messages_correlation_id");
    }
}
