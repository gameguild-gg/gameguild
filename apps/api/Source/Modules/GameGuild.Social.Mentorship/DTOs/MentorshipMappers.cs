using Microsoft.EntityFrameworkCore;

namespace GameGuild.Social.Mentorship;

internal static class MentorshipMappers
{
    public static MentorSkillDto ToDto(MentorSkill skill) => new(skill.Id, skill.Skill);

    public static MentorProfileDto ToDto(MentorProfile profile, IReadOnlyList<MentorSkill> skills) =>
        new(
            profile.Id,
            profile.UserId,
            profile.DisplayName,
            profile.Headline,
            profile.Bio,
            profile.Role,
            profile.PricingModel,
            profile.FreeContributionReason,
            profile.Status,
            profile.BookingMode,
            profile.OffersIntroductoryCall,
            profile.IntroductoryCallMinutes,
            profile.TimezoneId,
            skills.Select(ToDto).ToArray(),
            profile.PublishedAt,
            profile.CreatedAt,
            profile.UpdatedAt);

    public static MentorAvailabilitySlotDto ToDto(MentorAvailabilitySlot slot) =>
        new(slot.Id, slot.MentorProfileId, slot.StartsAt, slot.EndsAt, slot.Status);

    public static MentorshipSessionDto ToDto(MentorshipSession session) =>
        new(
            session.Id,
            session.MentorProfileId,
            session.MentorUserId,
            session.MenteeUserId,
            session.SlotId,
            session.Topic,
            session.Notes,
            session.StartsAt,
            session.EndsAt,
            session.Status,
            session.MeetingChannel,
            session.MeetingUrl,
            session.IsIntroductory,
            session.CancellationReason,
            session.CancelledAt,
            session.CompletedAt,
            session.CreatedAt);

    public static async Task<IReadOnlyList<MentorSkill>> LoadSkillsAsync(
        IApplicationDbContext context,
        Guid profileId,
        CancellationToken cancellationToken)
    {
        var skills = await context.Set<MentorSkill>()
            .AsNoTracking()
            .Where(skill => skill.MentorProfileId == profileId)
            .OrderBy(skill => skill.NormalizedSkill)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return skills;
    }

    public static async Task<IReadOnlyDictionary<Guid, IReadOnlyList<MentorSkill>>> LoadSkillsAsync(
        IApplicationDbContext context,
        IReadOnlyCollection<Guid> profileIds,
        CancellationToken cancellationToken)
    {
        var skills = await context.Set<MentorSkill>()
            .AsNoTracking()
            .Where(skill => profileIds.Contains(skill.MentorProfileId))
            .OrderBy(skill => skill.NormalizedSkill)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return skills
            .GroupBy(skill => skill.MentorProfileId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<MentorSkill>)group.ToArray());
    }
}
