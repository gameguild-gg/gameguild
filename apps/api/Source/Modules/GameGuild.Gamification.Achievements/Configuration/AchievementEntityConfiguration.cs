using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Gamification.Achievements.Configuration;

/// <summary>
/// Entity Framework Core configuration for Achievement entities.
/// </summary>
public class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> builder)
    {
        builder.ToTable("achievements");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .HasMaxLength(2000);

        builder.Property(a => a.Category)
            .HasMaxLength(100);

        builder.Property(a => a.Type)
            .HasMaxLength(100);

        builder.Property(a => a.IconUrl)
            .HasMaxLength(500);

        builder.Property(a => a.Color)
            .HasMaxLength(50);

        builder.Property(a => a.Conditions)
            .HasColumnType("jsonb");

        builder.HasIndex(a => a.Category);
        builder.HasIndex(a => a.IsActive);
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => new { a.Category, a.IsActive });

        // Relationships
        builder.HasMany(a => a.UserAchievements)
            .WithOne(ua => ua.Achievement)
            .HasForeignKey(ua => ua.AchievementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Levels)
            .WithOne(l => l.Achievement)
            .HasForeignKey(l => l.AchievementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Prerequisites)
            .WithOne(p => p.Achievement)
            .HasForeignKey(p => p.AchievementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserAchievementConfiguration : IEntityTypeConfiguration<UserAchievement>
{
    public void Configure(EntityTypeBuilder<UserAchievement> builder)
    {
        builder.ToTable("user_achievements");

        builder.HasKey(ua => ua.Id);

        builder.Property(ua => ua.Context)
            .HasColumnType("jsonb");

        builder.HasIndex(ua => ua.UserId);
        builder.HasIndex(ua => ua.AchievementId);
        builder.HasIndex(ua => ua.EarnedAt);
        builder.HasIndex(ua => ua.TenantId);
        builder.HasIndex(ua => new { ua.UserId, ua.AchievementId });
        builder.HasIndex(ua => new { ua.UserId, ua.IsNotified });
    }
}

public class AchievementLevelConfiguration : IEntityTypeConfiguration<AchievementLevel>
{
    public void Configure(EntityTypeBuilder<AchievementLevel> builder)
    {
        builder.ToTable("achievement_levels");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .HasMaxLength(100);

        builder.Property(l => l.IconUrl)
            .HasMaxLength(500);

        builder.HasIndex(l => new { l.AchievementId, l.Level })
            .IsUnique();
    }
}

public class AchievementPrerequisiteConfiguration : IEntityTypeConfiguration<AchievementPrerequisite>
{
    public void Configure(EntityTypeBuilder<AchievementPrerequisite> builder)
    {
        builder.ToTable("achievement_prerequisites");

        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.AchievementId, p.PrerequisiteAchievementId })
            .IsUnique();
    }
}

public class AchievementProgressConfiguration : IEntityTypeConfiguration<AchievementProgress>
{
    public void Configure(EntityTypeBuilder<AchievementProgress> builder)
    {
        builder.ToTable("achievement_progress");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Context)
            .HasMaxLength(500);

        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.AchievementId);
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => new { p.UserId, p.AchievementId })
            .IsUnique();
    }
}
