using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Users;

/// <summary>
///     Entity Framework configuration for UserPreferences
/// </summary>
public class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        // Configure relationships — specify inverse navigation (u.Preferences)
        // to avoid EF creating a shadow UserId1 FK.
        builder.HasOne(up => up.User).WithOne(u => u.Preferences).HasForeignKey<UserPreferences>(up => up.UserId).OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        builder.HasIndex(up => up.UserId).IsUnique();

        // JSON column types are defined via [Column(TypeName = "jsonb")] attributes
        builder.Property(up => up.GeneralPreferences).IsRequired();
        builder.Property(up => up.NotificationPreferences).IsRequired();
        builder.Property(up => up.AccessibilityPreferences).IsRequired();
        builder.Property(up => up.PrivacyPreferences).IsRequired();
    }
}
