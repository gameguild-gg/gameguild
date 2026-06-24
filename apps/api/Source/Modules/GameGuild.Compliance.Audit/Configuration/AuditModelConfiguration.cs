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
    }
}
