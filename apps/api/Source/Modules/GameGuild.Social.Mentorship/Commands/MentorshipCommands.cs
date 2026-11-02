using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Social.Mentorship;

// ─────────────────────────── Profile commands ───────────────────────────

public sealed record UpsertMentorProfileCommand(
    Guid UserId,
    string DisplayName,
    string? Headline,
    string? Bio,
    MentorshipProgramRole Role,
    MentorPricingModel PricingModel,
    string? FreeContributionReason,
    MentorBookingMode BookingMode,
    bool OffersIntroductoryCall,
    int IntroductoryCallMinutes,
    string TimezoneId,
    IReadOnlyList<string> Skills,
    Guid? TenantId = null) : ICommand<MentorProfileDto>;

public sealed class UpsertMentorProfileCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UpsertMentorProfileCommand, MentorProfileDto>
{
    private const int MaxSkills = 20;

    public async Task<MentorProfileDto> Handle(UpsertMentorProfileCommand request, CancellationToken cancellationToken)
    {
        var normalizedSkills = NormalizeSkills(request.Skills);

        var profile = await context.Set<MentorProfile>()
            .SingleOrDefaultAsync(profile => profile.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            profile = MentorProfile.Create(
                request.UserId,
                request.DisplayName,
                request.Headline,
                request.Bio,
                request.Role,
                request.PricingModel,
                request.FreeContributionReason,
                request.BookingMode,
                request.OffersIntroductoryCall,
                request.IntroductoryCallMinutes,
                request.TimezoneId,
                request.TenantId);
            context.Set<MentorProfile>().Add(profile);
        }
        else
        {
            profile.Update(
                request.DisplayName,
                request.Headline,
                request.Bio,
                request.Role,
                request.PricingModel,
                request.FreeContributionReason,
                request.BookingMode,
                request.OffersIntroductoryCall,
                request.IntroductoryCallMinutes,
                request.TimezoneId);
        }

        await ReplaceSkillsAsync(profile.Id, normalizedSkills, cancellationToken).ConfigureAwait(false);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var skills = await MentorshipMappers.LoadSkillsAsync(context, profile.Id, cancellationToken).ConfigureAwait(false);
        return MentorshipMappers.ToDto(profile, skills);
    }

    private static IReadOnlyList<string> NormalizeSkills(IReadOnlyList<string>? skills)
    {
        if (skills is null || skills.Count == 0)
        {
            return [];
        }

        var normalized = skills
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Select(skill => skill.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxSkills)
            .ToArray();

        if (skills.Count > MaxSkills)
        {
            throw new RequestValidationException($"A mentor profile can list at most {MaxSkills} skills.");
        }

        return normalized;
    }

    private async Task ReplaceSkillsAsync(Guid profileId, IReadOnlyList<string> skills, CancellationToken cancellationToken)
    {
        var existing = await context.Set<MentorSkill>()
            .Where(skill => skill.MentorProfileId == profileId)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        context.Set<MentorSkill>().RemoveRange(existing);

        foreach (var skill in skills)
        {
            context.Set<MentorSkill>().Add(MentorSkill.Create(profileId, skill));
        }
    }

    internal static Task<IReadOnlyList<MentorSkill>> LoadSkillsAsync(
        IApplicationDbContext context,
        Guid profileId,
        CancellationToken cancellationToken)
        => MentorshipMappers.LoadSkillsAsync(context, profileId, cancellationToken);
}

public sealed record SetMentorProfileStatusCommand(Guid UserId, MentorProfileStatus Status)
    : ICommand<MentorProfileDto>;

public sealed class SetMentorProfileStatusCommandHandler(IApplicationDbContext context)
    : ICommandHandler<SetMentorProfileStatusCommand, MentorProfileDto>
{
    public async Task<MentorProfileDto> Handle(SetMentorProfileStatusCommand request, CancellationToken cancellationToken)
    {
        var profile = await GetOwnedProfileAsync(context, request.UserId, cancellationToken).ConfigureAwait(false);

        switch (request.Status)
        {
            case MentorProfileStatus.Draft:
                profile.RevertToDraft();
                break;
            case MentorProfileStatus.Published:
                profile.Publish();
                break;
            case MentorProfileStatus.Paused:
                profile.Pause();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.Status), request.Status, null);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var skills = await MentorshipMappers.LoadSkillsAsync(context, profile.Id, cancellationToken)
            .ConfigureAwait(false);
        return MentorshipMappers.ToDto(profile, skills);
    }

    internal static async Task<MentorProfile> GetOwnedProfileAsync(
        IApplicationDbContext context,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var profile = await context.Set<MentorProfile>()
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            throw new RequestValidationException("Create a mentor profile first.");
        }

        return profile;
    }
}

// ─────────────────────────── Availability commands ───────────────────────────

public sealed record PublishMentorAvailabilityCommand(
    Guid UserId,
    IReadOnlyList<MentorAvailabilityRangeRequest> Slots) : ICommand<IReadOnlyList<MentorAvailabilitySlotDto>>;

public sealed class PublishMentorAvailabilityCommandHandler(IApplicationDbContext context)
    : ICommandHandler<PublishMentorAvailabilityCommand, IReadOnlyList<MentorAvailabilitySlotDto>>
{
    private const int MaxSlotsPerRequest = 50;
    private static readonly TimeSpan MinimumLeadTime = TimeSpan.FromHours(1);
    private static readonly TimeSpan MinimumSlotDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan MaximumSlotDuration = TimeSpan.FromHours(8);

    public async Task<IReadOnlyList<MentorAvailabilitySlotDto>> Handle(
        PublishMentorAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Slots.Count == 0)
        {
            throw new RequestValidationException("At least one availability range is required.");
        }

        if (request.Slots.Count > MaxSlotsPerRequest)
        {
            throw new RequestValidationException($"At most {MaxSlotsPerRequest} availability ranges can be published per request.");
        }

        var profile = await SetMentorProfileStatusCommandHandler.GetOwnedProfileAsync(
                context, request.UserId, cancellationToken)
            .ConfigureAwait(false);

        var openSlots = await context.Set<MentorAvailabilitySlot>()
            .Where(slot => slot.MentorProfileId == profile.Id && slot.Status == MentorAvailabilitySlotStatus.Open)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var now = SystemClock.UtcNow;
        var created = new List<MentorAvailabilitySlot>(request.Slots.Count);

        foreach (var range in request.Slots)
        {
            var slot = MentorAvailabilitySlot.Create(
                profile.Id,
                profile.UserId,
                DateTime.SpecifyKind(range.StartsAt, DateTimeKind.Utc),
                DateTime.SpecifyKind(range.EndsAt, DateTimeKind.Utc));

            if (slot.StartsAt < now + MinimumLeadTime)
            {
                throw new RequestValidationException("Availability must start at least one hour from now.");
            }

            var duration = slot.EndsAt - slot.StartsAt;
            if (duration < MinimumSlotDuration || duration > MaximumSlotDuration)
            {
                throw new RequestValidationException("Availability must be between 10 minutes and 8 hours long.");
            }

            if (openSlots.Any(existing => existing.Overlaps(slot.StartsAt, slot.EndsAt)) ||
                created.Any(existing => existing.Overlaps(slot.StartsAt, slot.EndsAt)))
            {
                throw new RequestValidationException("Availability ranges must not overlap existing open availability.");
            }

            created.Add(slot);
            context.Set<MentorAvailabilitySlot>().Add(slot);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return created
            .OrderBy(slot => slot.StartsAt)
            .Select(MentorshipMappers.ToDto)
            .ToArray();
    }
}

public sealed record RemoveMentorAvailabilitySlotCommand(Guid UserId, Guid SlotId) : ICommand<bool>;

public sealed class RemoveMentorAvailabilitySlotCommandHandler(IApplicationDbContext context)
    : ICommandHandler<RemoveMentorAvailabilitySlotCommand, bool>
{
    public async Task<bool> Handle(RemoveMentorAvailabilitySlotCommand request, CancellationToken cancellationToken)
    {
        var slot = await context.Set<MentorAvailabilitySlot>()
            .SingleOrDefaultAsync(slot => slot.Id == request.SlotId && slot.MentorUserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (slot is null || slot.Status == MentorAvailabilitySlotStatus.Cancelled)
        {
            return false;
        }

        if (slot.Status == MentorAvailabilitySlotStatus.Booked)
        {
            throw new RequestValidationException("A booked availability slot cannot be removed; cancel the session first.");
        }

        slot.Cancel();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}

// ─────────────────────────── Session commands ───────────────────────────

public sealed record BookMentorshipSessionCommand(
    Guid ActingUserId,
    Guid? MenteeUserId,
    Guid SlotId,
    string Topic,
    string? Notes,
    MentorshipMeetingChannel MeetingChannel) : ICommand<MentorshipSessionDto>;

public sealed class BookMentorshipSessionCommandHandler(IApplicationDbContext context)
    : ICommandHandler<BookMentorshipSessionCommand, MentorshipSessionDto>
{
    public async Task<MentorshipSessionDto> Handle(BookMentorshipSessionCommand request, CancellationToken cancellationToken)
    {
        var slot = await context.Set<MentorAvailabilitySlot>()
            .SingleOrDefaultAsync(slot => slot.Id == request.SlotId, cancellationToken)
            .ConfigureAwait(false);

        if (slot is null || slot.Status != MentorAvailabilitySlotStatus.Open)
        {
            throw new RequestValidationException("The selected availability slot is no longer open.");
        }

        var profile = await context.Set<MentorProfile>()
            .SingleOrDefaultAsync(profile => profile.Id == slot.MentorProfileId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null || profile.Status != MentorProfileStatus.Published)
        {
            throw new RequestValidationException("The mentor is not currently accepting bookings.");
        }

        if (slot.StartsAt < SystemClock.UtcNow)
        {
            throw new RequestValidationException("The selected availability slot has already started.");
        }

        Guid menteeUserId;

        if (profile.BookingMode == MentorBookingMode.MatchmakerOnly)
        {
            var actingAsMatchmaker = await IsPublishedMatchmakerAsync(context, request.ActingUserId, cancellationToken)
                .ConfigureAwait(false);

            if (!actingAsMatchmaker)
            {
                throw new RequestValidationException(
                    "This mentor's schedule is managed by a matchmaker; ask a matchmaker to book the session.");
            }

            if (!request.MenteeUserId.HasValue)
            {
                throw new RequestValidationException("A matchmaker booking must name the advisee.");
            }

            menteeUserId = request.MenteeUserId.Value;
        }
        else if (request.MenteeUserId.HasValue && request.MenteeUserId.Value != request.ActingUserId)
        {
            if (!await IsPublishedMatchmakerAsync(context, request.ActingUserId, cancellationToken).ConfigureAwait(false))
            {
                throw new RequestValidationException("Only a matchmaker can book a session for another advisee.");
            }

            menteeUserId = request.MenteeUserId.Value;
        }
        else
        {
            menteeUserId = request.ActingUserId;
        }

        var duration = slot.EndsAt - slot.StartsAt;
        var isIntroductory =
            profile.OffersIntroductoryCall &&
            duration.TotalMinutes <= profile.IntroductoryCallMinutes;

        var session = MentorshipSession.Book(
            profile.Id,
            profile.UserId,
            menteeUserId,
            slot.Id,
            request.Topic,
            request.Notes,
            slot.StartsAt,
            slot.EndsAt,
            request.MeetingChannel,
            isIntroductory);

        slot.Book(session.Id);
        context.Set<MentorshipSession>().Add(session);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MentorshipMappers.ToDto(session);
    }

    private static async Task<bool> IsPublishedMatchmakerAsync(
        IApplicationDbContext context,
        Guid userId,
        CancellationToken cancellationToken)
        => await context.Set<MentorProfile>()
            .AsNoTracking()
            .AnyAsync(profile =>
                profile.UserId == userId &&
                profile.Role == MentorshipProgramRole.Matchmaker &&
                profile.Status == MentorProfileStatus.Published,
                cancellationToken)
            .ConfigureAwait(false);
}

public sealed record CancelMentorshipSessionCommand(Guid UserId, Guid SessionId, string? Reason)
    : ICommand<MentorshipSessionDto>;

public sealed class CancelMentorshipSessionCommandHandler(IApplicationDbContext context)
    : ICommandHandler<CancelMentorshipSessionCommand, MentorshipSessionDto>
{
    public async Task<MentorshipSessionDto> Handle(CancelMentorshipSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await GetParticipantSessionAsync(context, request.UserId, request.SessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session.Status != MentorshipSessionStatus.Booked)
        {
            throw new RequestValidationException("Only a booked session can be cancelled.");
        }

        session.Cancel(request.UserId, request.Reason);

        if (session.SlotId.HasValue)
        {
            var slot = await context.Set<MentorAvailabilitySlot>()
                .SingleOrDefaultAsync(slot => slot.Id == session.SlotId.Value, cancellationToken)
                .ConfigureAwait(false);
            if (slot is not null && slot.SessionId == session.Id)
            {
                slot.Release();
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MentorshipMappers.ToDto(session);
    }

    internal static async Task<MentorshipSession> GetParticipantSessionAsync(
        IApplicationDbContext context,
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await context.Set<MentorshipSession>()
            .SingleOrDefaultAsync(session => session.Id == sessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null || (session.MentorUserId != userId && session.MenteeUserId != userId))
        {
            throw new RequestValidationException("Mentorship session not found for the current user.");
        }

        return session;
    }
}

public sealed record CompleteMentorshipSessionCommand(Guid UserId, Guid SessionId) : ICommand<MentorshipSessionDto>;

public sealed class CompleteMentorshipSessionCommandHandler(IApplicationDbContext context)
    : ICommandHandler<CompleteMentorshipSessionCommand, MentorshipSessionDto>
{
    public async Task<MentorshipSessionDto> Handle(CompleteMentorshipSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await CancelMentorshipSessionCommandHandler.GetParticipantSessionAsync(
                context, request.UserId, request.SessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session.MentorUserId != request.UserId)
        {
            throw new RequestValidationException("Only the mentor can complete a session.");
        }

        session.Complete();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MentorshipMappers.ToDto(session);
    }
}
