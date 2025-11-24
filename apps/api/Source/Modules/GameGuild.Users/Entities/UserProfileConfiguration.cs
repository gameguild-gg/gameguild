using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Users.Entities;

/// <summary>
///     Entity Framework configuration for UserProfile
/// </summary>
public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        // Configure relationships
        builder.HasOne(up => up.User).WithOne().HasForeignKey<UserProfile>(up => up.UserId).OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        builder.HasIndex(up => up.UserId).IsUnique();

        // Configure enums
        builder.Property(up => up.Visibility).HasConversion<int>();

        // Configure value conversions
        builder.Property(up => up.DateOfBirth).HasConversion<DateOnly>();
    }
}
