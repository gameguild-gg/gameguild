using GameGuild.Modules.Users;
using GameGuild.Modules.Followers.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.Followers.Configuration;

public class FollowerPrivacySettingsConfiguration : IEntityTypeConfiguration<FollowerPrivacySettings>
{
    public void Configure(EntityTypeBuilder<FollowerPrivacySettings> builder)
    {
        builder.ToTable("FollowerPrivacySettings");

        builder.HasKey(fps => fps.Id);

        builder.HasIndex(fps => fps.UserId)
            .IsUnique();

        builder.Property(fps => fps.IsFollowerListPublic)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(fps => fps.IsFollowingListPublic)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(fps => fps.AllowFollowers)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(fps => fps.NotifyOnNewFollower)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(fps => fps.ShowFollowerCount)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(fps => fps.ShowFollowingCount)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(fps => fps.User)
            .WithMany()
            .HasForeignKey(fps => fps.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
