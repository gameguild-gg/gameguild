using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     EF Core mapping for the JWT signing-key rotation store (issue #1141). The entity
///     existed with no model mapping, table or migration, so the rotation store was
///     unreachable and rotating keys never changed what signed tokens.
/// </summary>
public sealed class JwtSigningKeyConfiguration : IEntityTypeConfiguration<JwtSigningKey>
{
    public void Configure(EntityTypeBuilder<JwtSigningKey> builder)
    {
        builder.ToTable("jwt_signing_keys", "gameguild.authentication");

        builder.HasKey(key => key.Id);
        builder.Property(key => key.Id).HasColumnName("id").ValueGeneratedNever();

        // kid lookups during token validation are point queries on key_id.
        builder.Property(key => key.KeyId).HasColumnName("key_id").HasMaxLength(50).IsRequired();
        builder.HasIndex(key => key.KeyId).IsUnique().HasDatabaseName("ux_jwt_signing_keys_key_id");

        builder.Property(key => key.KeyMaterial).HasColumnName("key_material").HasMaxLength(1000).IsRequired();
        builder.Property(key => key.Algorithm).HasColumnName("algorithm").HasMaxLength(20).IsRequired();
        builder.Property(key => key.IsActive).HasColumnName("is_active").IsRequired();
        builder.HasIndex(key => key.IsActive).HasDatabaseName("ix_jwt_signing_keys_is_active");
        builder.Property(key => key.ValidFrom).HasColumnName("valid_from").IsRequired();
        builder.Property(key => key.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.HasIndex(key => key.ExpiresAt).HasDatabaseName("ix_jwt_signing_keys_expires_at");
        builder.Property(key => key.RotatedAt).HasColumnName("rotated_at");
        builder.Property(key => key.RotationReason).HasColumnName("rotation_reason").HasMaxLength(200);
        builder.Property(key => key.KeyVersion).HasColumnName("key_version").IsRequired();

        // Signing keys are global platform configuration, never tenant-scoped.
        builder.Property(key => key.TenantId).HasColumnName("tenant_id");
        builder.Property(key => key.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Property(key => key.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(key => key.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(key => key.DeletedAt).HasColumnName("deleted_at");
    }
}
