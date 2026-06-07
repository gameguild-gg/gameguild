using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Social.Profiles;

public sealed class SocialProfileConfiguration : IEntityTypeConfiguration<SocialProfile>
{
    public void Configure(EntityTypeBuilder<SocialProfile> builder)
    {
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Handle).HasMaxLength(80);
        builder.Property(profile => profile.DisplayName).HasMaxLength(180);
        builder.Property(profile => profile.Bio).HasMaxLength(2000);
        builder.Property(profile => profile.AvatarUrl).HasMaxLength(500);
        builder.Property(profile => profile.BannerUrl).HasMaxLength(500);
        builder.Property(profile => profile.Headline).HasMaxLength(120);
        builder.Property(profile => profile.Location).HasMaxLength(120);
        builder.Property(profile => profile.TimeZone).HasMaxLength(80);
        builder.Property(profile => profile.WebsiteUrl).HasMaxLength(500);
        builder.Property(profile => profile.SocialLinksJson).HasColumnType("jsonb");
        builder.Property(profile => profile.Visibility).HasConversion<string>().HasMaxLength(40);
        builder.Property(profile => profile.AvailabilityStatus).HasConversion<string>().HasMaxLength(40);
        builder.HasMany(profile => profile.Skills)
            .WithOne(skill => skill.Profile)
            .HasForeignKey(skill => skill.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(profile => profile.PortfolioItems)
            .WithOne(item => item.Profile)
            .HasForeignKey(item => item.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProfileSkillConfiguration : IEntityTypeConfiguration<ProfileSkill>
{
    public void Configure(EntityTypeBuilder<ProfileSkill> builder)
    {
        builder.HasKey(skill => skill.Id);
        builder.Property(skill => skill.Name).HasMaxLength(120);
        builder.Property(skill => skill.Proficiency).HasConversion<string>().HasMaxLength(40);
    }
}

public sealed class ProfilePortfolioItemConfiguration : IEntityTypeConfiguration<ProfilePortfolioItem>
{
    public void Configure(EntityTypeBuilder<ProfilePortfolioItem> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Title).HasMaxLength(200);
        builder.Property(item => item.Description).HasMaxLength(2000);
        builder.Property(item => item.Url).HasMaxLength(500);
        builder.Property(item => item.ImageUrl).HasMaxLength(500);
    }
}
