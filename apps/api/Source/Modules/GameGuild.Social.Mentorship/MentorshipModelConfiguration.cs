using Microsoft.EntityFrameworkCore;

namespace GameGuild.Social.Mentorship;

/// <summary>
///     EF Core model configuration for the mentorship program (issue #102).
/// </summary>
public sealed class MentorshipModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MentorProfile>(entity =>
        {
            entity.ToTable("social_mentor_profiles");
            entity.HasKey(profile => profile.Id);
            entity.Property(profile => profile.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(profile => profile.Headline).HasMaxLength(200);
            entity.Property(profile => profile.Bio).HasMaxLength(2000);
            entity.Property(profile => profile.Role).HasConversion<int>();
            entity.Property(profile => profile.PricingModel).HasConversion<int>();
            entity.Property(profile => profile.FreeContributionReason).HasMaxLength(500);
            entity.Property(profile => profile.Status).HasConversion<int>();
            entity.Property(profile => profile.BookingMode).HasConversion<int>();
            entity.Property(profile => profile.TimezoneId).HasMaxLength(100).IsRequired();
            entity.HasIndex(profile => profile.UserId).IsUnique();
            entity.HasIndex(profile => new { profile.Status, profile.Role });
            entity.HasIndex(profile => profile.TenantId);
        });

        modelBuilder.Entity<MentorSkill>(entity =>
        {
            entity.ToTable("social_mentor_skills");
            entity.HasKey(skill => skill.Id);
            entity.Property(skill => skill.Skill).HasMaxLength(80).IsRequired();
            entity.Property(skill => skill.NormalizedSkill).HasMaxLength(80).IsRequired();
            entity.HasIndex(skill => new { skill.MentorProfileId, skill.NormalizedSkill }).IsUnique();
            entity.HasIndex(skill => skill.NormalizedSkill);
            entity.HasOne<MentorProfile>()
                .WithMany()
                .HasForeignKey(skill => skill.MentorProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MentorAvailabilitySlot>(entity =>
        {
            entity.ToTable("social_mentor_availability_slots");
            entity.HasKey(slot => slot.Id);
            entity.Property(slot => slot.Status).HasConversion<int>();
            entity.HasIndex(slot => new { slot.MentorProfileId, slot.StartsAt });
            entity.HasIndex(slot => new { slot.Status, slot.StartsAt });
            entity.HasIndex(slot => slot.MentorUserId);
            entity.HasIndex(slot => slot.SessionId);
            entity.HasOne<MentorProfile>()
                .WithMany()
                .HasForeignKey(slot => slot.MentorProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MentorshipSession>(entity =>
        {
            entity.ToTable("social_mentorship_sessions");
            entity.HasKey(session => session.Id);
            entity.Property(session => session.Topic).HasMaxLength(200).IsRequired();
            entity.Property(session => session.Notes).HasMaxLength(2000);
            entity.Property(session => session.Status).HasConversion<int>();
            entity.Property(session => session.MeetingChannel).HasConversion<int>();
            entity.Property(session => session.MeetingUrl).HasMaxLength(2000);
            entity.Property(session => session.CancellationReason).HasMaxLength(500);
            entity.HasIndex(session => new { session.MentorUserId, session.StartsAt });
            entity.HasIndex(session => new { session.MenteeUserId, session.StartsAt });
            entity.HasIndex(session => session.SlotId);
            entity.HasOne<MentorAvailabilitySlot>()
                .WithMany()
                .HasForeignKey(session => session.SlotId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
