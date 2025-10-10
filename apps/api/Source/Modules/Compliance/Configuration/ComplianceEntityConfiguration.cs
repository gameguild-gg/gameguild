using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Modules.Compliance.Entities;

namespace GameGuild.Modules.Compliance.Configuration;

public class ConsentPolicyConfiguration : IEntityTypeConfiguration<ConsentPolicy>
{
    public void Configure(EntityTypeBuilder<ConsentPolicy> builder)
    {
        builder.ToTable("ConsentPolicies");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasIndex(p => new { p.TenantId, p.Type });
        builder.HasIndex(p => p.IsActive);

        builder.HasMany(p => p.Versions)
            .WithOne(v => v.Policy)
            .HasForeignKey(v => v.PolicyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.CurrentVersion)
            .WithMany()
            .HasForeignKey(p => p.CurrentVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PolicyVersionConfiguration : IEntityTypeConfiguration<PolicyVersion>
{
    public void Configure(EntityTypeBuilder<PolicyVersion> builder)
    {
        builder.ToTable("PolicyVersions");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.VersionNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(v => v.Content)
            .IsRequired();

        builder.Property(v => v.ContentType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(v => v.ChangeLog)
            .HasMaxLength(2000);

        builder.HasIndex(v => new { v.PolicyId, v.VersionNumber });
        builder.HasIndex(v => v.IsCurrent);
    }
}

public class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> builder)
    {
        builder.ToTable("UserConsents");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.IpAddress)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(c => c.UserAgent)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.ConsentMethod)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Metadata)
            .HasMaxLength(4000);

        builder.Property(c => c.WithdrawalReason)
            .HasMaxLength(1000);

        builder.HasIndex(c => new { c.UserId, c.PolicyId });
        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => c.ConsentedAt);

        builder.HasOne(c => c.Policy)
            .WithMany()
            .HasForeignKey(c => c.PolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.PolicyVersion)
            .WithMany()
            .HasForeignKey(c => c.PolicyVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ComplianceAuditConfiguration : IEntityTypeConfiguration<ComplianceAudit>
{
    public void Configure(EntityTypeBuilder<ComplianceAudit> builder)
    {
        builder.ToTable("ComplianceAudits");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EventType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.OldValues)
            .HasColumnType("jsonb");

        builder.Property(a => a.NewValues)
            .HasColumnType("jsonb");

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45);

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500);

        builder.Property(a => a.Metadata)
            .HasColumnType("jsonb");

        builder.Property(a => a.Regulation)
            .HasMaxLength(50);

        builder.Property(a => a.Severity)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.EventType);
        builder.HasIndex(a => a.OccurredAt);
        builder.HasIndex(a => a.Severity);
    }
}
