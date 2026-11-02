using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Social.Mentorship;

// ─────────────────────────── Mentor directory queries ───────────────────────────

public sealed record ListMentorsQuery(
    string? Skill = null,
    MentorshipProgramRole? Role = null,
    string? Search = null,
    bool? OffersIntroductoryCall = null,
    int Skip = 0,
    int Take = 20) : IQuery<IReadOnlyList<MentorProfileDto>>;

public sealed class ListMentorsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<ListMentorsQuery, IReadOnlyList<MentorProfileDto>>
{
    public async Task<IReadOnlyList<MentorProfileDto>> Handle(ListMentorsQuery request, CancellationToken cancellationToken)
    {
        var profiles = context.Set<MentorProfile>()
            .AsNoTracking()
            .Where(profile => profile.Status == MentorProfileStatus.Published);

        if (request.Role.HasValue)
        {
            profiles = profiles.Where(profile => profile.Role == request.Role.Value);
        }

        if (request.OffersIntroductoryCall.HasValue)
        {
            profiles = profiles.Where(profile => profile.OffersIntroductoryCall == request.OffersIntroductoryCall.Value);
        }

        var normalizedSkill = request.Skill?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(normalizedSkill))
        {
            profiles = profiles.Where(profile =>
                context.Set<MentorSkill>().Any(skill =>
                    skill.MentorProfileId == profile.Id &&
                    skill.NormalizedSkill == normalizedSkill));
        }

        var search = request.Search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(search))
        {
            profiles = profiles.Where(profile =>
                profile.DisplayName.ToLower().Contains(search) ||
                (profile.Headline != null && profile.Headline.ToLower().Contains(search)));
        }

        var matched = await profiles
            .OrderByDescending(profile => profile.PublishedAt)
            .ThenBy(profile => profile.DisplayName)
            .Skip(Math.Max(0, request.Skip))
            .Take(Math.Clamp(request.Take <= 0 ? 20 : request.Take, 1, 100))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var skills = await MentorshipMappers.LoadSkillsAsync(
                context, matched.Select(profile => profile.Id).ToArray(), cancellationToken)
            .ConfigureAwait(false);

        return matched
            .Select(profile => MentorshipMappers.ToDto(
                profile,
                skills.GetValueOrDefault(profile.Id, [])))
            .ToArray();
    }
}

public sealed record GetMentorQuery(Guid ProfileId, Guid? ViewerUserId = null) : IQuery<MentorProfileDto?>;

public sealed class GetMentorQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetMentorQuery, MentorProfileDto?>
{
    public async Task<MentorProfileDto?> Handle(GetMentorQuery request, CancellationToken cancellationToken)
    {
        var profile = await context.Set<MentorProfile>()
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.Id == request.ProfileId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            return null;
        }

        // Unpublished profiles are only visible to their owner (invariant 10:
        // unpublished content is not public).
        if (profile.Status != MentorProfileStatus.Published && profile.UserId != request.ViewerUserId)
        {
            return null;
        }

        var skills = await MentorshipMappers.LoadSkillsAsync(context, profile.Id, cancellationToken)
            .ConfigureAwait(false);
        return MentorshipMappers.ToDto(profile, skills);
    }
}

public sealed record ListMentorAvailabilityQuery(
    Guid ProfileId,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    bool IncludeBooked = false) : IQuery<IReadOnlyList<MentorAvailabilitySlotDto>>;

public sealed class ListMentorAvailabilityQueryHandler(IApplicationDbContext context)
    : IQueryHandler<ListMentorAvailabilityQuery, IReadOnlyList<MentorAvailabilitySlotDto>>
{
    public async Task<IReadOnlyList<MentorAvailabilitySlotDto>> Handle(
        ListMentorAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        var slots = context.Set<MentorAvailabilitySlot>()
            .AsNoTracking()
            .Where(slot => slot.MentorProfileId == request.ProfileId);

        if (request.IncludeBooked)
        {
            slots = slots.Where(slot => slot.Status != MentorAvailabilitySlotStatus.Cancelled);
        }
        else
        {
            slots = slots.Where(slot => slot.Status == MentorAvailabilitySlotStatus.Open);
        }

        if (request.FromUtc.HasValue)
        {
            var from = DateTime.SpecifyKind(request.FromUtc.Value, DateTimeKind.Utc);
            slots = slots.Where(slot => slot.EndsAt >= from);
        }

        if (request.ToUtc.HasValue)
        {
            var to = DateTime.SpecifyKind(request.ToUtc.Value, DateTimeKind.Utc);
            slots = slots.Where(slot => slot.StartsAt <= to);
        }

        var matched = await slots
            .OrderBy(slot => slot.StartsAt)
            .Take(200)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return matched.Select(MentorshipMappers.ToDto).ToArray();
    }
}

// ─────────────────────────── Self-service queries ───────────────────────────

public sealed record GetMyMentorProfileQuery(Guid UserId) : IQuery<MentorProfileDto?>;

public sealed class GetMyMentorProfileQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetMyMentorProfileQuery, MentorProfileDto?>
{
    public async Task<MentorProfileDto?> Handle(GetMyMentorProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await context.Set<MentorProfile>()
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            return null;
        }

        var skills = await MentorshipMappers.LoadSkillsAsync(context, profile.Id, cancellationToken)
            .ConfigureAwait(false);
        return MentorshipMappers.ToDto(profile, skills);
    }
}

public sealed record ListMyMentorshipSessionsQuery(
    Guid UserId,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    MentorshipSessionStatus? Status = null,
    bool AsMentorOnly = false) : IQuery<IReadOnlyList<MentorshipSessionDto>>;

public sealed class ListMyMentorshipSessionsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<ListMyMentorshipSessionsQuery, IReadOnlyList<MentorshipSessionDto>>
{
    public async Task<IReadOnlyList<MentorshipSessionDto>> Handle(
        ListMyMentorshipSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var sessions = context.Set<MentorshipSession>()
            .AsNoTracking()
            .Where(session => request.AsMentorOnly
                ? session.MentorUserId == request.UserId
                : session.MentorUserId == request.UserId || session.MenteeUserId == request.UserId);

        if (request.FromUtc.HasValue)
        {
            var from = DateTime.SpecifyKind(request.FromUtc.Value, DateTimeKind.Utc);
            sessions = sessions.Where(session => session.EndsAt >= from);
        }

        if (request.ToUtc.HasValue)
        {
            var to = DateTime.SpecifyKind(request.ToUtc.Value, DateTimeKind.Utc);
            sessions = sessions.Where(session => session.StartsAt <= to);
        }

        if (request.Status.HasValue)
        {
            sessions = sessions.Where(session => session.Status == request.Status.Value);
        }

        var matched = await sessions
            .OrderBy(session => session.StartsAt)
            .Take(200)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return matched.Select(MentorshipMappers.ToDto).ToArray();
    }
}

public sealed record GetMentorDashboardQuery(Guid UserId) : IQuery<MentorDashboardDto>;

public sealed class GetMentorDashboardQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetMentorDashboardQuery, MentorDashboardDto>
{
    public async Task<MentorDashboardDto> Handle(GetMentorDashboardQuery request, CancellationToken cancellationToken)
    {
        var profile = await context.Set<MentorProfile>()
            .AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        MentorProfileDto? profileDto = null;
        if (profile is not null)
        {
            var skills = await MentorshipMappers.LoadSkillsAsync(context, profile.Id, cancellationToken)
                .ConfigureAwait(false);
            profileDto = MentorshipMappers.ToDto(profile, skills);
        }

        var now = SystemClock.UtcNow;

        var upcoming = await context.Set<MentorshipSession>()
            .AsNoTracking()
            .Where(session => session.MentorUserId == request.UserId &&
                              session.Status == MentorshipSessionStatus.Booked &&
                              session.StartsAt >= now)
            .OrderBy(session => session.StartsAt)
            .Take(10)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var openSlotCount = profile is null
            ? 0
            : await context.Set<MentorAvailabilitySlot>()
                .AsNoTracking()
                .CountAsync(slot =>
                        slot.MentorProfileId == profile.Id &&
                        slot.Status == MentorAvailabilitySlotStatus.Open &&
                        slot.StartsAt >= now,
                    cancellationToken)
                .ConfigureAwait(false);

        var totalSessionCount = await context.Set<MentorshipSession>()
            .AsNoTracking()
            .CountAsync(session => session.MentorUserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        var completedSessionCount = await context.Set<MentorshipSession>()
            .AsNoTracking()
            .CountAsync(session =>
                    session.MentorUserId == request.UserId &&
                    session.Status == MentorshipSessionStatus.Completed,
                cancellationToken)
            .ConfigureAwait(false);

        return new MentorDashboardDto(
            profileDto,
            upcoming.Count,
            openSlotCount,
            completedSessionCount,
            totalSessionCount,
            upcoming.Select(MentorshipMappers.ToDto).ToArray());
    }
}
