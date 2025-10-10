using GameGuild.Modules.Followers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.Followers.Configuration;

public class BlockedUserConfiguration : IEntityTypeConfiguration<BlockedUser>
{
    public void Configure(EntityTypeBuilder<BlockedUser> builder)
    {
        builder.ToTable("BlockedUsers");

        builder.HasKey(bu => bu.Id);

        builder.HasIndex(bu => new { bu.BlockingUserId, bu.BlockedUserId })
            .IsUnique();

        builder.HasIndex(bu => bu.BlockedUserId);

        builder.Property(bu => bu.Reason)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(bu => bu.BlockedAt)
            .IsRequired();

        builder.HasOne(bu => bu.BlockingUser)
            .WithMany()
            .HasForeignKey(bu => bu.BlockingUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(bu => bu.BlockedUserEntity)
            .WithMany()
            .HasForeignKey(bu => bu.BlockedUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
