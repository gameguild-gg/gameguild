using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Social.Follows.Configuration;

public class FollowEntityConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.ToTable("follows");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.FollowerId)
            .IsRequired();

        builder.Property(f => f.FollowedEntityId)
            .IsRequired();

        builder.Property(f => f.FollowedEntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.NotificationsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(f => f.FollowedAt)
            .IsRequired();

        // Unique constraint: a user can only follow an entity once
        builder.HasIndex(f => new { f.FollowerId, f.FollowedEntityId, f.FollowedEntityType })
            .IsUnique();

        // Index for querying followers of an entity
        builder.HasIndex(f => new { f.FollowedEntityId, f.FollowedEntityType });

        // Index for querying what a user is following
        builder.HasIndex(f => f.FollowerId);
    }
}

public class BlockEntityConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        builder.ToTable("blocks");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BlockerId)
            .IsRequired();

        builder.Property(b => b.BlockedId)
            .IsRequired();

        builder.Property(b => b.Reason)
            .HasMaxLength(500);

        builder.Property(b => b.BlockedAt)
            .IsRequired();

        // Unique constraint: a user can only block another user once
        builder.HasIndex(b => new { b.BlockerId, b.BlockedId })
            .IsUnique();

        // Index for checking if a user is blocked
        builder.HasIndex(b => b.BlockedId);
    }
}

public class MuteEntityConfiguration : IEntityTypeConfiguration<Mute>
{
    public void Configure(EntityTypeBuilder<Mute> builder)
    {
        builder.ToTable("mutes");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.MuterId)
            .IsRequired();

        builder.Property(m => m.MutedId)
            .IsRequired();

        builder.Property(m => m.Reason)
            .HasMaxLength(500);

        builder.Property(m => m.MutedAt)
            .IsRequired();

        builder.Property(m => m.ExpiresAt);

        // Unique constraint: a user can only mute another user once
        builder.HasIndex(m => new { m.MuterId, m.MutedId })
            .IsUnique();

        // Index for checking if a user is muted
        builder.HasIndex(m => m.MutedId);

        // Index for cleanup of expired mutes
        builder.HasIndex(m => m.ExpiresAt)
            .HasFilter("\"ExpiresAt\" IS NOT NULL");
    }
}

public class FollowPrivacySettingsEntityConfiguration : IEntityTypeConfiguration<FollowPrivacySettings>
{
    public void Configure(EntityTypeBuilder<FollowPrivacySettings> builder)
    {
        builder.ToTable("follow_privacy_settings");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.IsFollowerListPublic)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.IsFollowingListPublic)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.AllowFollowers)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.NotifyOnNewFollower)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.ShowFollowerCount)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.ShowFollowingCount)
            .IsRequired()
            .HasDefaultValue(true);

        // One settings record per user
        builder.HasIndex(p => p.UserId)
            .IsUnique();
    }
}
