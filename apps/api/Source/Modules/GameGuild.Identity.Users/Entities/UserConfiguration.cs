using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Users;

/// <summary>
///     Entity Type Configuration for User
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure Id property
        builder.Property(x => x.Id).IsRequired();

        // ========================
        // NAVIGATION PROPERTIES
        // ========================

        // User → UserProfile (1:1 optional)
        builder.HasOne(x => x.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // User → UserMetadata (1:1 optional)
        builder.HasOne(x => x.Metadata)
            .WithOne(m => m.User)
            .HasForeignKey<UserMetadata>(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // User → UserPreferences (1:1 optional)
        builder.HasOne(x => x.Preferences)
            .WithOne(p => p.User)
            .HasForeignKey<UserPreferences>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // User → UserNotifications (1:many)
        builder.HasMany(x => x.Notifications)
            .WithOne(n => n.User)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ========================
        // IGNORED PROPERTIES
        // ========================

        // Status is a computed NotMapped property
        builder.Ignore(x => x.Status);
    }
}
