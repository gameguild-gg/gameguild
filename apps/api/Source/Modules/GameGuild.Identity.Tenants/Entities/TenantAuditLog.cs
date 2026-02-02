using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Entity for tracking tenant audit log entries
/// </summary>
public class TenantAuditLog : EntityBase
{
    /// <summary>
    ///     Tenant navigation property
    /// </summary>
    public virtual Tenant? Tenant { get; set; }

    /// <summary>
    ///     Timestamp when the action occurred
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    ///     Type of action performed (e.g., 'create', 'update', 'delete', 'settings_change')
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    ///     ID of the user who performed the action (null for system actions)
    /// </summary>
    public Guid? ActorId { get; set; }

    /// <summary>
    ///     Name of the actor at the time of the action
    /// </summary>
    public string? ActorName { get; set; }

    /// <summary>
    ///     Email of the actor at the time of the action
    /// </summary>
    public string? ActorEmail { get; set; }

    /// <summary>
    ///     Values before the change (JSON serialized)
    /// </summary>
    public Dictionary<string, object?>? BeforeValues { get; set; }

    /// <summary>
    ///     Values after the change (JSON serialized)
    /// </summary>
    public Dictionary<string, object?>? AfterValues { get; set; }

    /// <summary>
    ///     IP address of the request
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    ///     User agent of the request
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    ///     Correlation ID for request tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    ///     Additional metadata (JSON serialized)
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
///     Entity Framework configuration for TenantAuditLog
/// </summary>
public class TenantAuditLogConfiguration : IEntityTypeConfiguration<TenantAuditLog>
{
    public void Configure(EntityTypeBuilder<TenantAuditLog> builder)
    {
        builder.ToTable("TenantAuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Timestamp)
            .IsRequired();

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ActorName)
            .HasMaxLength(256);

        builder.Property(x => x.ActorEmail)
            .HasMaxLength(256);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45); // Max length for IPv6

        builder.Property(x => x.UserAgent)
            .HasMaxLength(500);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);

        // Configure JSON columns - PostgreSQL natively supports jsonb
        builder.Property(x => x.BeforeValues)
            .HasColumnType("jsonb");

        builder.Property(x => x.AfterValues)
            .HasColumnType("jsonb");

        builder.Property(x => x.Metadata)
            .HasColumnType("jsonb");

        // Relationships
        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for common queries
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => x.Action);
        builder.HasIndex(x => x.ActorId);
        builder.HasIndex(x => new { x.TenantId, x.Timestamp });
    }
}
