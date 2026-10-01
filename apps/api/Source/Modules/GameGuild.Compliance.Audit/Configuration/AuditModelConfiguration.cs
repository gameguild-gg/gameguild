using Microsoft.EntityFrameworkCore;

namespace GameGuild.Compliance.Audit;

public sealed class AuditModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(log => log.Id);

            entity.Property(log => log.ActionType).HasMaxLength(100).IsRequired();
            entity.Property(log => log.ResourceType).HasMaxLength(100).IsRequired();
            entity.Property(log => log.ResourceId).HasMaxLength(100);
            entity.Property(log => log.IpAddress).HasMaxLength(45);
            entity.Property(log => log.UserAgent).HasMaxLength(500);
            entity.Property(log => log.Description).HasMaxLength(1000);
            entity.Property(log => log.ErrorMessage).HasMaxLength(500);
            entity.Property(log => log.CorrelationId).HasMaxLength(100);
            entity.Property(log => log.RiskLevel).HasConversion<int>();
            entity.Property(log => log.Category).HasConversion<int>();

            entity.HasIndex(log => log.ActionType);
            entity.HasIndex(log => log.ResourceType);
            entity.HasIndex(log => log.ResourceId);
            entity.HasIndex(log => log.UserId);
            entity.HasIndex(log => log.TenantId);
            entity.HasIndex(log => log.CreatedAt);
            entity.HasIndex(log => new { log.TenantId, log.CreatedAt });
        });

        modelBuilder.Entity<TamperEvidentAuditLog>(entity =>
        {
            entity.ToTable("TamperEvidentAuditLogs");
            entity.HasKey(log => log.Id);

            entity.Property(log => log.TenantId).IsRequired();
            entity.Property(log => log.Action).HasMaxLength(100).IsRequired();
            entity.Property(log => log.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(log => log.RiskLevel).HasMaxLength(50).IsRequired();
            entity.Property(log => log.IpAddress).HasMaxLength(45).IsRequired();
            entity.Property(log => log.UserAgent).HasMaxLength(500).IsRequired();
            entity.Property(log => log.CorrelationId).HasMaxLength(100);
            entity.Property(log => log.Country).HasMaxLength(100);
            entity.Property(log => log.Region).HasMaxLength(100);
            entity.Property(log => log.City).HasMaxLength(100);
            entity.Property(log => log.ContentHash).HasMaxLength(64).IsRequired();
            entity.Property(log => log.PreviousHash).HasMaxLength(64).IsRequired();
            entity.Property(log => log.ChainHash).HasMaxLength(64).IsRequired();
            entity.Property(log => log.DigitalSignature).HasMaxLength(256).IsRequired();
            entity.Property(log => log.SigningKeyId).HasMaxLength(100).IsRequired();
            entity.Property(log => log.CustodyChain).HasColumnType("text");
            entity.Property(log => log.BeforeSnapshot).HasColumnType("text");
            entity.Property(log => log.AfterSnapshot).HasColumnType("text");
            entity.Property(log => log.Changes).HasColumnType("text").IsRequired();
            entity.Property(log => log.VerificationNotes).HasColumnType("text");

            entity.HasIndex(log => new { log.TenantId, log.SequenceNumber }).IsUnique();
            entity.HasIndex(log => new { log.TenantId, log.Timestamp });
            entity.HasIndex(log => new { log.TenantId, log.Action, log.Timestamp });
            entity.HasIndex(log => new { log.TenantId, log.SessionId, log.Timestamp });
        });
    }
}
