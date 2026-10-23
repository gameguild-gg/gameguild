using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Authentication;

public sealed class SamlProtocolStateConfiguration : IEntityTypeConfiguration<SamlProtocolState>
{
    public void Configure(EntityTypeBuilder<SamlProtocolState> builder)
    {
        builder.ToTable("saml_protocol_state", "gameguild.authentication", table => table.HasCheckConstraint(
            "ck_saml_protocol_state_kind",
            "state_kind IN ('pending_request', 'assertion_reservation')"));
        builder.HasKey(row => new { row.StateKind, row.ProviderSlug, row.StateKey });
        builder.Property(row => row.StateKind).HasColumnName("state_kind").HasMaxLength(32).IsRequired();
        builder.Property(row => row.ProviderSlug).HasColumnName("provider_slug").HasMaxLength(64).IsRequired();
        builder.Property(row => row.StateKey).HasColumnName("state_key").HasMaxLength(256).IsRequired();
        builder.Property(row => row.RelayStateFingerprint).HasColumnName("relay_state_fingerprint")
            .HasMaxLength(64).IsRequired().HasDefaultValue(string.Empty);
        builder.Property(row => row.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(row => row.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasIndex(row => row.ExpiresAt).HasDatabaseName("ix_saml_protocol_state_expires_at");
    }
}
