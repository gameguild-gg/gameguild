using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Entity Type Configuration for TenantSecurityVersion.
/// </summary>
public class TenantSecurityVersionConfiguration : IEntityTypeConfiguration<TenantSecurityVersion>
{
    public void Configure(EntityTypeBuilder<TenantSecurityVersion> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).IsRequired();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.SecurityVersion)
            .IsRequired();

        builder.Property(x => x.LastUpdatedAt)
            .IsRequired();

        builder.Property(x => x.LastChangeReason)
            .HasMaxLength(500);
    }
}
