using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Social.Profiles;

/// <summary>
///     Entity Framework configuration for UserProfile
/// </summary>
public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        // Configure indexes
        builder.HasIndex(up => up.UserId).IsUnique();

        // Configure enums
        builder.Property(up => up.Visibility).HasConversion<int>();

        // Configure value conversions
        builder.Property(up => up.DateOfBirth).HasConversion<DateOnly>();
    }
}
