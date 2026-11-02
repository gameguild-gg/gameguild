namespace GameGuild.Social.Mentorship;

public enum MentorshipProgramRole
{
    Mentor = 0,
    Advisor = 1,
    Matchmaker = 2,
}

public enum MentorPricingModel
{
    Free = 0,
    Paid = 1,
}

public enum MentorProfileStatus
{
    Draft = 0,
    Published = 1,
    Paused = 2,
}

public enum MentorBookingMode
{
    /// <summary>Any advisee can book any published open slot directly.</summary>
    Public = 0,

    /// <summary>Only a matchmaker can assign sessions; slots are visible but not self-bookable.</summary>
    MatchmakerOnly = 1,
}

public enum MentorAvailabilitySlotStatus
{
    Open = 0,
    Booked = 1,
    Cancelled = 2,
}

public enum MentorshipSessionStatus
{
    Booked = 0,
    Cancelled = 1,
    Completed = 2,
}

public enum MentorshipMeetingChannel
{
    InPlatform = 0,
    Discord = 1,
    GoogleCalendar = 2,
}

/// <summary>
///     A mentor, advisor, or onboarding matchmaker profile (issue #102). The owner
///     publishes availability slots; advisees book introductory sessions against them.
/// </summary>
public sealed class MentorProfile : EntityBase
{
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string? Headline { get; private set; }
    public string? Bio { get; private set; }
    public MentorshipProgramRole Role { get; private set; }
    public MentorPricingModel PricingModel { get; private set; }

    /// <summary>Required by issue #102 whenever the pricing model is free.</summary>
    public string? FreeContributionReason { get; private set; }

    public MentorProfileStatus Status { get; private set; }
    public MentorBookingMode BookingMode { get; private set; }

    /// <summary>Offers the free 10-15 minute introductory consultation described in issue #102.</summary>
    public bool OffersIntroductoryCall { get; private set; }
    public int IntroductoryCallMinutes { get; private set; }
    public string TimezoneId { get; private set; } = "UTC";
    public DateTime? PublishedAt { get; private set; }
    public new Guid? TenantId { get; private set; }

    private MentorProfile() { }

    public static MentorProfile Create(
        Guid userId,
        string displayName,
        string? headline,
        string? bio,
        MentorshipProgramRole role,
        MentorPricingModel pricingModel,
        string? freeContributionReason,
        MentorBookingMode bookingMode,
        bool offersIntroductoryCall,
        int introductoryCallMinutes,
        string timezoneId,
        Guid? tenantId = null)
    {
        ValidatePricing(pricingModel, freeContributionReason);

        return new MentorProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = NormalizeRequired(displayName, nameof(displayName), 120),
            Headline = NormalizeOptional(headline, 200),
            Bio = NormalizeOptional(bio, 2000),
            Role = role,
            PricingModel = pricingModel,
            FreeContributionReason = NormalizeOptional(freeContributionReason, 500),
            Status = MentorProfileStatus.Draft,
            BookingMode = bookingMode,
            OffersIntroductoryCall = offersIntroductoryCall,
            IntroductoryCallMinutes = NormalizeIntroMinutes(introductoryCallMinutes),
            TimezoneId = NormalizeTimezone(timezoneId),
            TenantId = tenantId,
        };
    }

    public void Update(
        string displayName,
        string? headline,
        string? bio,
        MentorshipProgramRole role,
        MentorPricingModel pricingModel,
        string? freeContributionReason,
        MentorBookingMode bookingMode,
        bool offersIntroductoryCall,
        int introductoryCallMinutes,
        string timezoneId)
    {
        ValidatePricing(pricingModel, freeContributionReason);

        DisplayName = NormalizeRequired(displayName, nameof(displayName), 120);
        Headline = NormalizeOptional(headline, 200);
        Bio = NormalizeOptional(bio, 2000);
        Role = role;
        PricingModel = pricingModel;
        FreeContributionReason = NormalizeOptional(freeContributionReason, 500);
        BookingMode = bookingMode;
        OffersIntroductoryCall = offersIntroductoryCall;
        IntroductoryCallMinutes = NormalizeIntroMinutes(introductoryCallMinutes);
        TimezoneId = NormalizeTimezone(timezoneId);
        Touch();
    }

    public void Publish()
    {
        Status = MentorProfileStatus.Published;
        PublishedAt ??= SystemClock.UtcNow;
        Touch();
    }

    public void Pause()
    {
        Status = MentorProfileStatus.Paused;
        Touch();
    }

    public void RevertToDraft()
    {
        Status = MentorProfileStatus.Draft;
        Touch();
    }

    private static void ValidatePricing(MentorPricingModel pricingModel, string? freeContributionReason)
    {
        if (pricingModel == MentorPricingModel.Free && string.IsNullOrWhiteSpace(freeContributionReason))
        {
            throw new ArgumentException(
                "A mentor offering sessions for free must state the reason for the free contribution.",
                nameof(freeContributionReason));
        }
    }

    private static int NormalizeIntroMinutes(int introductoryCallMinutes)
    {
        if (introductoryCallMinutes <= 0)
        {
            return 15;
        }

        return Math.Clamp(introductoryCallMinutes, 10, 15);
    }

    private static string NormalizeTimezone(string timezoneId) =>
        string.IsNullOrWhiteSpace(timezoneId) ? "UTC" : timezoneId.Trim().Truncate(100);

    private static string NormalizeRequired(string value, string fieldName, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException($"{fieldName} is required.", fieldName);
        }

        return trimmed.Truncate(maxLength);
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed.Truncate(maxLength);
    }
}

/// <summary>Skill tag used by advisees to find a mentor by skill (issue #102 matching).</summary>
public sealed class MentorSkill : EntityBase
{
    public Guid MentorProfileId { get; private set; }
    public string Skill { get; private set; } = string.Empty;
    public string NormalizedSkill { get; private set; } = string.Empty;

    private MentorSkill() { }

    public static MentorSkill Create(Guid mentorProfileId, string skill)
    {
        var trimmed = skill?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("Skill is required.", nameof(skill));
        }

        return new MentorSkill
        {
            Id = Guid.NewGuid(),
            MentorProfileId = mentorProfileId,
            Skill = trimmed.Truncate(80),
            NormalizedSkill = trimmed.Trim().ToLowerInvariant().Truncate(80),
        };
    }
}

/// <summary>A published block of availability a mentor offers for booking.</summary>
public sealed class MentorAvailabilitySlot : EntityBase
{
    public Guid MentorProfileId { get; private set; }
    public Guid MentorUserId { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }
    public MentorAvailabilitySlotStatus Status { get; private set; }
    public Guid? SessionId { get; private set; }

    private MentorAvailabilitySlot() { }

    public static MentorAvailabilitySlot Create(Guid mentorProfileId, Guid mentorUserId, DateTime startsAt, DateTime endsAt)
    {
        ValidateRange(startsAt, endsAt);

        return new MentorAvailabilitySlot
        {
            Id = Guid.NewGuid(),
            MentorProfileId = mentorProfileId,
            MentorUserId = mentorUserId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Status = MentorAvailabilitySlotStatus.Open,
        };
    }

    public bool Overlaps(DateTime startsAt, DateTime endsAt) =>
        StartsAt < endsAt && startsAt < EndsAt;

    public void Book(Guid sessionId)
    {
        if (Status != MentorAvailabilitySlotStatus.Open)
        {
            throw new InvalidOperationException("Only an open availability slot can be booked.");
        }

        Status = MentorAvailabilitySlotStatus.Booked;
        SessionId = sessionId;
        Touch();
    }

    public void Release()
    {
        if (Status != MentorAvailabilitySlotStatus.Booked)
        {
            return;
        }

        Status = MentorAvailabilitySlotStatus.Open;
        SessionId = null;
        Touch();
    }

    public void Cancel()
    {
        if (Status == MentorAvailabilitySlotStatus.Cancelled)
        {
            return;
        }

        Status = MentorAvailabilitySlotStatus.Cancelled;
        SessionId = null;
        Touch();
    }

    private static void ValidateRange(DateTime startsAt, DateTime endsAt)
    {
        if (startsAt == default || endsAt == default)
        {
            throw new ArgumentException("Availability start and end are required.");
        }

        if (endsAt <= startsAt)
        {
            throw new ArgumentException("The availability end must be later than its start.", nameof(endsAt));
        }

        if (startsAt.Kind != DateTimeKind.Utc || endsAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Availability must be expressed in UTC.");
        }
    }
}

/// <summary>A booked meeting between a mentor (or matchmaker) and an advisee (issue #102).</summary>
public sealed class MentorshipSession : EntityBase
{
    public Guid MentorProfileId { get; private set; }
    public Guid MentorUserId { get; private set; }
    public Guid MenteeUserId { get; private set; }
    public Guid? SlotId { get; private set; }
    public string Topic { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }
    public MentorshipSessionStatus Status { get; private set; }
    public MentorshipMeetingChannel MeetingChannel { get; private set; }
    public string? MeetingUrl { get; private set; }
    public bool IsIntroductory { get; private set; }
    public string? CancellationReason { get; private set; }
    public Guid? CancelledByUserId { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private MentorshipSession() { }

    public static MentorshipSession Book(
        Guid mentorProfileId,
        Guid mentorUserId,
        Guid menteeUserId,
        Guid? slotId,
        string topic,
        string? notes,
        DateTime startsAt,
        DateTime endsAt,
        MentorshipMeetingChannel meetingChannel,
        bool isIntroductory)
    {
        if (startsAt == default || endsAt == default || endsAt <= startsAt)
        {
            throw new ArgumentException("The session must have a valid UTC time range.", nameof(endsAt));
        }

        if (mentorUserId == menteeUserId)
        {
            throw new ArgumentException("A mentor cannot book a session with themselves.", nameof(menteeUserId));
        }

        return new MentorshipSession
        {
            Id = Guid.NewGuid(),
            MentorProfileId = mentorProfileId,
            MentorUserId = mentorUserId,
            MenteeUserId = menteeUserId,
            SlotId = slotId,
            Topic = string.IsNullOrWhiteSpace(topic)
                ? throw new ArgumentException("A session topic is required.", nameof(topic))
                : topic.Trim().Truncate(200),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim().Truncate(2000),
            StartsAt = startsAt,
            EndsAt = endsAt,
            Status = MentorshipSessionStatus.Booked,
            MeetingChannel = meetingChannel,
            IsIntroductory = isIntroductory,
        };
    }

    public void Cancel(Guid cancelledByUserId, string? reason)
    {
        if (Status != MentorshipSessionStatus.Booked)
        {
            throw new InvalidOperationException("Only a booked session can be cancelled.");
        }

        Status = MentorshipSessionStatus.Cancelled;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim().Truncate(500);
        CancelledByUserId = cancelledByUserId;
        CancelledAt = SystemClock.UtcNow;
        Touch();
    }

    public void Complete()
    {
        if (Status != MentorshipSessionStatus.Booked)
        {
            throw new InvalidOperationException("Only a booked session can be completed.");
        }

        Status = MentorshipSessionStatus.Completed;
        CompletedAt = SystemClock.UtcNow;
        Touch();
    }

    public void AssignMeetingDetails(MentorshipMeetingChannel meetingChannel, string? meetingUrl)
    {
        MeetingChannel = meetingChannel;
        MeetingUrl = string.IsNullOrWhiteSpace(meetingUrl) ? null : meetingUrl.Trim().Truncate(2000);
        Touch();
    }
}

internal static class StringTruncationExtensions
{
    public static string Truncate(this string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
