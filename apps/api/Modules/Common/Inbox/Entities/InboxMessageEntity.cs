namespace GameGuild.Modules.Common.Inbox.Entities;

/// <summary>
/// Entity for storing inbox messages (deduplication)
/// </summary>
public sealed class InboxMessageEntity
{
    public required string MessageId { get; init; }
    public required string MessageType { get; init; }
    public required DateTime ReceivedAt { get; init; }
    public required DateTime ProcessedAt { get; init; }
}

/// <summary>
/// EF Core configuration for InboxMessageEntity
/// </summary>
internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<InboxMessageEntity> builder)
    {
        builder.ToTable("inbox_messages", "common");

        builder.HasKey(x => x.MessageId);

        builder.Property(x => x.MessageId)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.MessageType)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ReceivedAt)
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .IsRequired();

        builder.HasIndex(x => x.ProcessedAt)
            .HasDatabaseName("ix_inbox_messages_processed_at");
    }
}
