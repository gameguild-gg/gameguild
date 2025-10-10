using GameGuild.Modules.Followers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.Followers.Configuration;

public class MutedUserConfiguration : IEntityTypeConfiguration<MutedUser>
{
    public void Configure(EntityTypeBuilder<MutedUser> builder)
    {
        builder.ToTable("MutedUsers");

        builder.HasKey(mu => mu.Id);

        builder.HasIndex(mu => new { mu.MutingUserId, mu.MutedUserId })
            .IsUnique();

        builder.HasIndex(mu => mu.MutedUserId);

        builder.HasIndex(mu => mu.ExpiresAt)
            .HasFilter("ExpiresAt IS NOT NULL");

        builder.Property(mu => mu.Reason)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(mu => mu.MutedAt)
            .IsRequired();

        builder.Property(mu => mu.ExpiresAt)
            .IsRequired(false);

        builder.HasOne(mu => mu.MutingUser)
            .WithMany()
            .HasForeignKey(mu => mu.MutingUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(mu => mu.MutedUserEntity)
            .WithMany()
            .HasForeignKey(mu => mu.MutedUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
