using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Users.Entities;

/// <summary>
///     Entity Framework configuration for UserPreferences
/// </summary>
public class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        // Configure relationships
        builder.HasOne(up => up.User).WithOne().HasForeignKey<UserPreferences>(up => up.UserId).OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        builder.HasIndex(up => up.UserId).IsUnique();

        // JSON column types are defined via [Column(TypeName = "jsonb")] attributes
        builder.Property(up => up.GeneralPreferences).IsRequired();
        builder.Property(up => up.NotificationPreferences).IsRequired();
        builder.Property(up => up.AccessibilityPreferences).IsRequired();
        builder.Property(up => up.PrivacyPreferences).IsRequired();
    }
}
