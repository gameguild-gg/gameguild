using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Entity Type Configuration for AccessControlListEntry.
/// </summary>
public class AccessControlListEntryConfiguration : IEntityTypeConfiguration<AccessControlListEntry>
{
    public void Configure(EntityTypeBuilder<AccessControlListEntry> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).IsRequired();

        builder.Property(x => x.TenantId)
            .IsRequired();

        // New principal-based properties
        builder.Property(x => x.PrincipalType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.PrincipalId)
            .IsRequired(false);

        builder.Property(x => x.IsDenied)
            .IsRequired();

        // Ignore deprecated UserId property (mapped via PrincipalId for User type)
#pragma warning disable CS0618 // Type or member is obsolete
        builder.Ignore(x => x.UserId);
#pragma warning restore CS0618

        builder.Property(x => x.ResourceType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ResourceId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.AccessLevel)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.GrantedBy)
            .IsRequired();

        builder.Property(x => x.GrantedAt)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        // Index for finding all entries for a specific resource
        builder.HasIndex(x => new { x.TenantId, x.ResourceType, x.ResourceId });

        // Index for finding entries for a specific principal on a resource
        builder.HasIndex(x => new { x.TenantId, x.PrincipalType, x.PrincipalId, x.ResourceType, x.ResourceId });

        // Index for deny-first evaluation (finding deny entries quickly)
        builder.HasIndex(x => new { x.TenantId, x.ResourceType, x.ResourceId, x.IsDenied });
    }
}
