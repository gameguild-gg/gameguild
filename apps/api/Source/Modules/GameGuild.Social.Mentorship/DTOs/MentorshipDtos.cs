namespace GameGuild.Social.Mentorship;

public sealed record MentorSkillDto(Guid Id, string Skill);

public sealed record MentorProfileDto(
    Guid Id,
    Guid UserId,
    string DisplayName,
    string? Headline,
    string? Bio,
    MentorshipProgramRole Role,
    MentorPricingModel PricingModel,
    string? FreeContributionReason,
    MentorProfileStatus Status,
    MentorBookingMode BookingMode,
    bool OffersIntroductoryCall,
    int IntroductoryCallMinutes,
    string TimezoneId,
    IReadOnlyList<MentorSkillDto> Skills,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record MentorAvailabilitySlotDto(
    Guid Id,
    Guid MentorProfileId,
    DateTime StartsAt,
    DateTime EndsAt,
    MentorAvailabilitySlotStatus Status);

public sealed record MentorshipSessionDto(
    Guid Id,
    Guid MentorProfileId,
    Guid MentorUserId,
    Guid MenteeUserId,
    Guid? SlotId,
    string Topic,
    string? Notes,
    DateTime StartsAt,
    DateTime EndsAt,
    MentorshipSessionStatus Status,
    MentorshipMeetingChannel MeetingChannel,
    string? MeetingUrl,
    bool IsIntroductory,
    string? CancellationReason,
    DateTime? CancelledAt,
    DateTime? CompletedAt,
    DateTime CreatedAt);

public sealed record MentorDashboardDto(
    MentorProfileDto? Profile,
    int UpcomingSessionCount,
    int OpenSlotCount,
    int CompletedSessionCount,
    int TotalSessionCount,
    IReadOnlyList<MentorshipSessionDto> UpcomingSessions);

public sealed record UpsertMentorProfileRequest(
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
    IReadOnlyList<string> Skills);

public sealed record SetMentorProfileStatusRequest(MentorProfileStatus Status);

public sealed record PublishMentorAvailabilityRequest(IReadOnlyList<MentorAvailabilityRangeRequest> Slots);

public sealed record MentorAvailabilityRangeRequest(DateTime StartsAt, DateTime EndsAt);

/// <summary>Public booking payload; MenteeUserId is only honored for published matchmakers.</summary>
public sealed record BookMentorshipSessionRequest(
    Guid SlotId,
    string Topic,
    string? Notes,
    Guid? MenteeUserId = null,
    MentorshipMeetingChannel MeetingChannel = MentorshipMeetingChannel.InPlatform);

public sealed record CancelMentorshipSessionRequest(string? Reason);
