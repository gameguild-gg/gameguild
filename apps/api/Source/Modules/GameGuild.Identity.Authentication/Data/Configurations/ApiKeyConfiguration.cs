using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Entity Type Configuration for API keys.
/// </summary>
public sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys", "gameguild.authentication");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.KeyHash)
            .HasColumnName("key_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.KeyPrefix)
            .HasColumnName("key_prefix")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Scopes)
            .HasColumnName("scopes")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at");

        builder.Property(x => x.LastUsedAt)
            .HasColumnName("last_used_at");

        builder.Property(x => x.UsageCount)
            .HasColumnName("usage_count")
            .HasDefaultValue(0L)
            .IsRequired();

        builder.Property(x => x.IpWhitelist)
            .HasColumnName("ip_whitelist")
            .HasMaxLength(100);

        builder.Property(x => x.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(x => x.RevocationReason)
            .HasColumnName("revocation_reason")
            .HasMaxLength(200);

        builder.Property(x => x.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasIndex(x => x.KeyHash)
            .IsUnique()
            .HasDatabaseName("ix_api_keys_key_hash");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_api_keys_user_id");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_api_keys_tenant_id");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("ix_api_keys_is_active");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("ix_api_keys_expires_at");

        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.IsGlobal);
        builder.Ignore(x => x.IsNew);
        builder.Ignore(x => x.IsDeleted);
    }
}
