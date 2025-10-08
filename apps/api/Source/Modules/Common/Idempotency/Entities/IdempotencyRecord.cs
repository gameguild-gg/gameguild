using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.Common.Idempotency.Entities;

/// <summary>
/// Entity for storing idempotent request results
/// </summary>
public sealed class IdempotencyRecord
{
    public required string IdempotencyKey { get; init; }
    public required string ResultJson { get; init; }
    public required int StatusCode { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime ExpiresAt { get; init; }
}

/// <summary>
/// EF Core configuration for IdempotencyRecord
/// </summary>
internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records", "common");

        builder.HasKey(x => x.IdempotencyKey);

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ResultJson)
            .IsRequired();

        builder.Property(x => x.StatusCode)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("ix_idempotency_records_expires_at");
    }
}
