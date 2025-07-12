using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GameGuild.Modules.Users;

namespace GameGuild.Modules.Credentials;

/// <summary>
/// Entity Framework configuration for Credential entity
/// Fixes relationship configuration to focus on types rather than GUID properties
/// </summary>
public class CredentialConfiguration : IEntityTypeConfiguration<Credential>
{
    public void Configure(EntityTypeBuilder<Credential> builder)
    {
        // Configure the relationship with User properly - focus on types, not GUIDs
        // This explicitly tells EF Core that Credential.User navigation property
        // corresponds to User.Credentials collection navigation property
        builder.HasOne(c => c.User)
               .WithMany(u => u.Credentials)
               .HasForeignKey(c => c.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
