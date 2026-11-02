using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace GameGuild.Social.Mentorship.UnitTests;

public class MentorshipEntityTests
{
    private static readonly Guid MentorUserId = Guid.NewGuid();
    private static readonly Guid MenteeUserId = Guid.NewGuid();

    [Fact]
    public void Create_RequiresFreeContributionReasonForFreeProgram()
    {
        var act = () => MentorProfile.Create(
            MentorUserId,
            "Grace",
            null,
            null,
            MentorshipProgramRole.Mentor,
            MentorPricingModel.Free,
            freeContributionReason: " ",
            bookingMode: MentorBookingMode.Public,
            offersIntroductoryCall: true,
            introductoryCallMinutes: 15,
            timezoneId: "UTC");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*free contribution*");
    }

    [Fact]
    public void Create_AllowsPaidProgramWithoutFreeReasonAndClampsIntroMinutes()
    {
        var profile = MentorProfile.Create(
            MentorUserId,
            "Grace",
            "Gameplay engineer",
            "Shipped a dozen titles.",
            MentorshipProgramRole.Advisor,
            MentorPricingModel.Paid,
            freeContributionReason: null,
            bookingMode: MentorBookingMode.MatchmakerOnly,
            offersIntroductoryCall: true,
            introductoryCallMinutes: 90,
            timezoneId: "America/Sao_Paulo");

        profile.Status.Should().Be(MentorProfileStatus.Draft);
        profile.BookingMode.Should().Be(MentorBookingMode.MatchmakerOnly);
        profile.IntroductoryCallMinutes.Should().Be(15);
        profile.TimezoneId.Should().Be("America/Sao_Paulo");
    }

    [Fact]
    public void Publish_SetsStatusAndFreezesFirstPublishedAt()
    {
        var profile = CreateProfile();
        profile.Publish();
        var firstPublishedAt = profile.PublishedAt;

        profile.Pause();
        profile.Publish();

        profile.Status.Should().Be(MentorProfileStatus.Published);
        profile.PublishedAt.Should().Be(firstPublishedAt);
    }

    [Fact]
    public void Skill_NormalizesForMatching()
    {
        var skill = MentorSkill.Create(Guid.NewGuid(), "  Level Design  ");

        skill.NormalizedSkill.Should().Be("level design");
        skill.Skill.Should().Be("Level Design");
    }

    [Fact]
    public void Slot_BookReleaseAndCancel_EnforceStatusRules()
    {
        var slot = CreateSlot();

        slot.Overlaps(slot.StartsAt.AddMinutes(5), slot.StartsAt.AddMinutes(20)).Should().BeTrue();
        slot.Overlaps(slot.EndsAt, slot.EndsAt.AddHours(1)).Should().BeFalse();

        var sessionId = Guid.NewGuid();
        slot.Book(sessionId);
        slot.Status.Should().Be(MentorAvailabilitySlotStatus.Booked);
        slot.SessionId.Should().Be(sessionId);

        var rebook = () => slot.Book(Guid.NewGuid());
        rebook.Should().Throw<InvalidOperationException>();

        slot.Release();
        slot.Status.Should().Be(MentorAvailabilitySlotStatus.Open);
        slot.SessionId.Should().BeNull();

        slot.Cancel();
        slot.Status.Should().Be(MentorAvailabilitySlotStatus.Cancelled);
    }

    [Fact]
    public void Slot_Create_RejectsInvalidRanges()
    {
        var start = DateTime.UtcNow.AddDays(1);
        Action reversed = () => MentorAvailabilitySlot.Create(Guid.NewGuid(), MentorUserId, start, start.AddMinutes(-30));
        Action nonUtc = () => MentorAvailabilitySlot.Create(
            Guid.NewGuid(), MentorUserId, DateTime.SpecifyKind(start, DateTimeKind.Local), start.AddHours(1));

        reversed.Should().Throw<ArgumentException>();
        nonUtc.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Session_BookCancelComplete_EnforceLifecycle()
    {
        var session = CreateSession();

        session.Status.Should().Be(MentorshipSessionStatus.Booked);
        session.IsIntroductory.Should().BeTrue();

        session.Complete();
        session.CompletedAt.Should().NotBeNull();

        var cancelAfterComplete = () => session.Cancel(MentorUserId, "late");
        cancelAfterComplete.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Session_Cancel_RecordsActorAndReason()
    {
        var session = CreateSession();

        session.Cancel(MenteeUserId, "Conflict came up");

        session.Status.Should().Be(MentorshipSessionStatus.Cancelled);
        session.CancelledByUserId.Should().Be(MenteeUserId);
        session.CancellationReason.Should().Be("Conflict came up");
        session.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void Session_Book_RejectsSelfMentoringAndBadRanges()
    {
        Action selfMentoring = () => CreateSession(menteeUserId: MentorUserId);
        selfMentoring.Should().Throw<ArgumentException>();

        Action emptyTopic = () => MentorshipSession.Book(
            Guid.NewGuid(), MentorUserId, MenteeUserId, null, " ", null,
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddMinutes(15),
            MentorshipMeetingChannel.InPlatform, isIntroductory: false);
        emptyTopic.Should().Throw<ArgumentException>();
    }

    private static MentorProfile CreateProfile() => MentorProfile.Create(
        MentorUserId,
        "Grace",
        null,
        null,
        MentorshipProgramRole.Mentor,
        MentorPricingModel.Free,
        "Giving back to the community",
        MentorBookingMode.Public,
        offersIntroductoryCall: true,
        introductoryCallMinutes: 15,
        timezoneId: "UTC");

    private static MentorAvailabilitySlot CreateSlot() => MentorAvailabilitySlot.Create(
        Guid.NewGuid(), MentorUserId, DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddMinutes(30));

    private static MentorshipSession CreateSession(Guid? menteeUserId = null) => MentorshipSession.Book(
        Guid.NewGuid(),
        MentorUserId,
        menteeUserId ?? MenteeUserId,
        Guid.NewGuid(),
        "First shader walkthrough",
        null,
        DateTime.UtcNow.AddDays(2),
        DateTime.UtcNow.AddDays(2).AddMinutes(15),
        MentorshipMeetingChannel.Discord,
        isIntroductory: true);
}

public class MentorshipAuthorizationTests
{
    [Fact]
    public void MentorProfileController_RequiresAuthenticationOnEveryEndpoint()
    {
        typeof(MentorProfileController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Should().ContainSingle();

        foreach (var methodName in new[]
                 {
                     nameof(MentorProfileController.GetMine),
                     nameof(MentorProfileController.Upsert),
                     nameof(MentorProfileController.SetStatus),
                     nameof(MentorProfileController.PublishAvailability),
                     nameof(MentorProfileController.RemoveAvailability),
                     nameof(MentorProfileController.Dashboard),
                 })
        {
            typeof(MentorProfileController).GetMethod(methodName)!
                .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
                .Should().BeEmpty($"{methodName} must rely on the controller-level [Authorize]");
        }
    }

    [Fact]
    public void MentorshipSessionsController_RequiresAuthenticationOnEveryEndpoint()
    {
        typeof(MentorshipSessionsController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Should().ContainSingle();

        foreach (var methodName in new[]
                 {
                     nameof(MentorshipSessionsController.List),
                     nameof(MentorshipSessionsController.Book),
                     nameof(MentorshipSessionsController.Cancel),
                     nameof(MentorshipSessionsController.Complete),
                 })
        {
            typeof(MentorshipSessionsController).GetMethod(methodName)!
                .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
                .Should().BeEmpty($"{methodName} must rely on the controller-level [Authorize]");
        }
    }

    [Fact]
    public void MentorsController_ExposesOnlyTheReviewedAnonymousReads()
    {
        typeof(MentorsController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Should().BeEmpty("the directory controller declares per-endpoint authorization");

        var anonymous = new[]
        {
            nameof(MentorsController.List),
            nameof(MentorsController.Get),
            nameof(MentorsController.Availability),
        };

        foreach (var methodName in anonymous)
        {
            typeof(MentorsController).GetMethod(methodName)!
                .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: false)
                .Should().ContainSingle($"{methodName} is a reviewed anonymous read");
        }
    }
}
