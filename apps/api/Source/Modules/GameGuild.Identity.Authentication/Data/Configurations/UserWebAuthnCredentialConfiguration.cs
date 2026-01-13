using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     EF Core configuration for UserWebAuthnCredential entity.
/// </summary>
public class UserWebAuthnCredentialConfiguration : IEntityTypeConfiguration<UserWebAuthnCredential>
{
    public void Configure(EntityTypeBuilder<UserWebAuthnCredential> builder)
    {
        builder.ToTable("UserWebAuthnCredentials", "auth");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CredentialId)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(c => c.PublicKey)
            .IsRequired();

        builder.Property(c => c.AaGuid)
            .HasMaxLength(36);

        builder.Property(c => c.FriendlyName)
            .HasMaxLength(100);

        builder.Property(c => c.CredentialType)
            .HasMaxLength(50)
            .HasDefaultValue("public-key");

        builder.Property(c => c.Transports)
            .HasMaxLength(200);

        builder.Property(c => c.RegisteredFromIp)
            .HasMaxLength(45);

        builder.Property(c => c.RegisteredUserAgent)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(c => c.UserId)
            .HasDatabaseName("IX_UserWebAuthnCredentials_UserId");

        builder.HasIndex(c => c.CredentialId)
            .IsUnique()
            .HasDatabaseName("IX_UserWebAuthnCredentials_CredentialId");

        builder.HasIndex(c => new { c.UserId, c.IsActive })
            .HasDatabaseName("IX_UserWebAuthnCredentials_UserId_IsActive");
    }
}
