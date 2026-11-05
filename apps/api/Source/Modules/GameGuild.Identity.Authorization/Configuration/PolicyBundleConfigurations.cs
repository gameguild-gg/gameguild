using GameGuild.CQRS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Authorization.Configuration;

/// <summary>
///     EF Core configuration for the central policy registry entities
///     (<see cref="PolicyBundle"/>, <see cref="PolicyBundleDeployment"/>,
///     <see cref="PolicyRegistryAuditLog"/>).
/// </summary>
/// <remarks>
///     G2-class wiring gap (issue #1278): the entities and their repository
///     (<c>PolicyBundleRepository</c>) existed, but no entity configuration ever
///     registered them in the EF model, so every <c>/v1/authorization/policy-bundles</c>
///     endpoint failed with "entity type PolicyBundle is not included in the model"
///     (500). Mapped here with the same conventions as the rest of the authorization
///     module: <c>TenantId</c> value conversion, enums persisted as <c>int</c>.
/// </remarks>
public class PolicyBundleConfiguration : IEntityTypeConfiguration<PolicyBundle>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PolicyBundle> builder)
    {
        builder.ToTable("PolicyBundles");

        builder.HasKey(bundle => bundle.Id);

        // TenantId value conversion (matches PermissionAuditLogConfiguration).
        builder.Property(bundle => bundle.TenantId)
            .HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new TenantId(v.Value) : null);

        builder.Property(bundle => bundle.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(bundle => bundle.Description)
            .HasMaxLength(2000);

        builder.Property(bundle => bundle.Version)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(bundle => bundle.BundleType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(bundle => bundle.ContentHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(bundle => bundle.SignedBy)
            .HasMaxLength(256);

        builder.Property(bundle => bundle.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(bundle => bundle.PolicyData)
            .IsRequired();

        // Previous-version chain: no FK constraint — historical bundles are retained
        // forever, and the chain is informational (audit), not referential.
        builder.HasIndex(bundle => bundle.PreviousVersionId)
            .HasDatabaseName("IX_PolicyBundles_PreviousVersion");

        builder.HasIndex(bundle => new { bundle.TenantId, bundle.Status })
            .HasDatabaseName("IX_PolicyBundles_Tenant_Status");

        builder.HasIndex(bundle => bundle.Name)
            .HasDatabaseName("IX_PolicyBundles_Name");
    }
}

/// <summary>
///     EF Core configuration for <see cref="PolicyBundleDeployment"/>.
/// </summary>
public class PolicyBundleDeploymentConfiguration : IEntityTypeConfiguration<PolicyBundleDeployment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PolicyBundleDeployment> builder)
    {
        builder.ToTable("PolicyBundleDeployments");

        builder.HasKey(deployment => deployment.Id);

        builder.Property(deployment => deployment.TenantId)
            .HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new TenantId(v.Value) : null);

        builder.Property(deployment => deployment.Environment)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(deployment => deployment.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(deployment => deployment.RollbackReason)
            .HasMaxLength(2000);

        builder.Property(deployment => deployment.VerificationDetails)
            .HasMaxLength(2000);

        builder.Property(deployment => deployment.DeploymentNotes)
            .HasMaxLength(2000);

        // Deployment history is retained for compliance; deleting a bundle never
        // cascades into its deployment records.
        builder.HasOne(deployment => deployment.Bundle)
            .WithMany()
            .HasForeignKey(deployment => deployment.BundleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(deployment => new { deployment.BundleId, deployment.DeployedAt })
            .HasDatabaseName("IX_PolicyBundleDeployments_Bundle_Time");

        builder.HasIndex(deployment => new { deployment.TenantId, deployment.Status })
            .HasDatabaseName("IX_PolicyBundleDeployments_Tenant_Status");
    }
}

/// <summary>
///     EF Core configuration for <see cref="PolicyRegistryAuditLog"/>.
/// </summary>
public class PolicyRegistryAuditConfiguration : IEntityTypeConfiguration<PolicyRegistryAuditLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PolicyRegistryAuditLog> builder)
    {
        builder.ToTable("PolicyRegistryAuditLogs");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Action)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(entry => entry.Details)
            .HasMaxLength(4000);

        builder.Property(entry => entry.IpAddress)
            .HasMaxLength(64);

        builder.Property(entry => entry.UserAgent)
            .HasMaxLength(512);

        builder.Property(entry => entry.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(entry => entry.PerformedAt)
            .IsRequired();

        builder.HasIndex(entry => entry.BundleId)
            .HasDatabaseName("IX_PolicyRegistryAuditLogs_Bundle");

        builder.HasIndex(entry => entry.PerformedAt)
            .HasDatabaseName("IX_PolicyRegistryAuditLogs_Time");
    }
}
