using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Users.Entities;

/// <summary>
///     Entity Framework configuration for UserNotification
/// </summary>
public class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        // Configure relationships
        builder.HasOne(un => un.User).WithMany().HasForeignKey(un => un.UserId).OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        builder.HasIndex(un => un.UserId);
        builder.HasIndex(un => un.Type);
        builder.HasIndex(un => un.IsRead);
        builder.HasIndex(un => un.Priority);
        builder.HasIndex(un => un.CreatedAt);
        builder.HasIndex(un => new { un.UserId, un.IsRead });
        builder.HasIndex(un => new { un.UserId, un.IsArchived });
        builder.HasIndex(un => new { un.UserId, un.Type, un.IsRead });

        // Configure enums
        builder.Property(un => un.Priority).HasConversion<int>();

        // JSON column type is defined via [Column(TypeName = "jsonb")] attribute
        builder.Property(un => un.Metadata).IsRequired();

        // Configure constraints
        builder.Property(un => un.Type).HasMaxLength(50).IsRequired();

        builder.Property(un => un.Title).HasMaxLength(200).IsRequired();

        builder.Property(un => un.Content).HasMaxLength(2000).IsRequired();

        builder.Property(un => un.Source).HasMaxLength(100);

        builder.Property(un => un.RelatedEntityType).HasMaxLength(100);

        builder.Property(un => un.ActionUrl).HasMaxLength(500);
    }
}
