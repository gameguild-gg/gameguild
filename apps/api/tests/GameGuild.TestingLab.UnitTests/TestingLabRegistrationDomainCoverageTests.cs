using FluentAssertions;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabRegistrationDomainCoverageTests
{
    [Fact]
    public void SlotRegistration_FactoryValidatesEveryIdentifierAndNormalizesPayload()
    {
        var eventId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Invoking(() => TestingSlotRegistration.Register(Guid.Empty, slotId, userId, null, null))
            .Should().Throw<ArgumentException>();
        Invoking(() => TestingSlotRegistration.Register(eventId, Guid.Empty, userId, null, null))
            .Should().Throw<ArgumentException>();
        Invoking(() => TestingSlotRegistration.Register(eventId, slotId, Guid.Empty, null, null))
            .Should().Throw<ArgumentException>();
        Invoking(() => TestingSlotRegistration.Waitlist(eventId, slotId, userId, 0, null, null))
            .Should().Throw<ArgumentOutOfRangeException>();

        var empty = TestingSlotRegistration.Register(eventId, slotId, userId, " ", null);
        var populated = TestingSlotRegistration.Register(eventId, slotId, userId, "  Notes  ", null);

        empty.Notes.Should().BeNull();
        populated.Notes.Should().Be("Notes");
        empty.RegistrationResponse.Should().NotBeNull();
        var uninitialized = (TestingSlotRegistration)Activator.CreateInstance(
            typeof(TestingSlotRegistration), nonPublic: true)!;
        uninitialized.RegistrationResponse.Should().BeNull();
    }

    [Fact]
    public void SlotRegistration_StateMachineAcceptsOnlyLegalTransitions()
    {
        var userId = Guid.NewGuid();
        var registered = Registered(userId);
        var waitlisted = Waitlisted(userId);

        Invoking(registered.Promote).Should().Throw<InvalidOperationException>();
        waitlisted.Promote();
        waitlisted.Status.Should().Be(TestingSlotRegistrationStatus.Registered);
        waitlisted.WaitlistPosition.Should().BeNull();

        Invoking(() => registered.Reposition(2)).Should().Throw<InvalidOperationException>();
        var repositioned = Waitlisted(userId);
        Invoking(() => repositioned.Reposition(0)).Should().Throw<ArgumentOutOfRangeException>();
        repositioned.Reposition(3);
        repositioned.WaitlistPosition.Should().Be(3);

        Invoking(() => registered.Cancel(Guid.NewGuid(), false)).Should().Throw<UnauthorizedAccessException>();
        registered.Cancel(userId, false);
        registered.Status.Should().Be(TestingSlotRegistrationStatus.Cancelled);
        Invoking(() => registered.Cancel(userId, false)).Should().Throw<InvalidOperationException>();

        var managerCancelled = Waitlisted(userId);
        managerCancelled.Cancel(Guid.NewGuid(), true);
        managerCancelled.Status.Should().Be(TestingSlotRegistrationStatus.Cancelled);

        var completed = Registered(userId);
        completed.CheckIn();
        completed.CheckOut();
        completed.Complete();
        Invoking(() => completed.Cancel(userId, false)).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SlotRegistration_AttendanceTransitionsRejectWrongStates()
    {
        var registered = Registered(Guid.NewGuid());
        var waitlisted = Waitlisted(Guid.NewGuid());

        Invoking(waitlisted.CheckIn).Should().Throw<InvalidOperationException>();
        Invoking(registered.CheckOut).Should().Throw<InvalidOperationException>();
        Invoking(waitlisted.MarkNoShow).Should().Throw<InvalidOperationException>();
        Invoking(registered.Complete).Should().Throw<InvalidOperationException>();

        registered.CheckIn();
        registered.Status.Should().Be(TestingSlotRegistrationStatus.CheckedIn);
        registered.CheckOut();
        registered.Status.Should().Be(TestingSlotRegistrationStatus.Attended);
        registered.Complete();
        registered.Status.Should().Be(TestingSlotRegistrationStatus.Completed);

        var noShow = Registered(Guid.NewGuid());
        noShow.MarkNoShow();
        noShow.Status.Should().Be(TestingSlotRegistrationStatus.NoShow);
    }

    [Fact]
    public void SlotRegistration_CapacityCoversEveryStatus()
    {
        var userId = Guid.NewGuid();
        var waitlisted = Waitlisted(userId);
        var cancelled = Waitlisted(userId);
        cancelled.Cancel(userId, false);
        var registered = Registered(userId);
        var checkedIn = Registered(userId);
        checkedIn.CheckIn();
        var attended = Registered(userId);
        attended.CheckIn();
        attended.CheckOut();
        var completed = Registered(userId);
        completed.CheckIn();
        completed.CheckOut();
        completed.Complete();
        var noShow = Registered(userId);
        noShow.MarkNoShow();

        waitlisted.ConsumesCapacity.Should().BeFalse();
        cancelled.ConsumesCapacity.Should().BeFalse();
        registered.ConsumesCapacity.Should().BeTrue();
        checkedIn.ConsumesCapacity.Should().BeTrue();
        attended.ConsumesCapacity.Should().BeTrue();
        completed.ConsumesCapacity.Should().BeTrue();
        noShow.ConsumesCapacity.Should().BeTrue();
    }

    [Fact]
    public void FeedbackObligation_ValidatesIdentifiersAndTerminalStates()
    {
        var eventId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Invoking(() => TestingFeedbackObligation.Create(
            Guid.Empty, slotId, applicationId, userId, null)).Should().Throw<ArgumentException>();
        Invoking(() => TestingFeedbackObligation.Create(
            eventId, Guid.Empty, applicationId, userId, null)).Should().Throw<ArgumentException>();
        Invoking(() => TestingFeedbackObligation.Create(
            eventId, slotId, Guid.Empty, userId, null)).Should().Throw<ArgumentException>();
        Invoking(() => TestingFeedbackObligation.Create(
            eventId, slotId, applicationId, Guid.Empty, null)).Should().Throw<ArgumentException>();

        var fulfilled = Obligation(eventId, slotId, applicationId, userId);
        fulfilled.IsFulfilled.Should().BeFalse();
        Invoking(() => fulfilled.Fulfill(Guid.Empty)).Should().Throw<ArgumentException>();
        fulfilled.Fulfill(Guid.NewGuid());
        fulfilled.IsFulfilled.Should().BeTrue();
        Invoking(() => fulfilled.Fulfill(Guid.NewGuid())).Should().Throw<InvalidOperationException>();
        Invoking(fulfilled.Waive).Should().Throw<InvalidOperationException>();

        var waived = Obligation(eventId, slotId, applicationId, userId);
        waived.Waive();
        waived.IsFulfilled.Should().BeTrue();
        Invoking(() => waived.Fulfill(Guid.NewGuid())).Should().Throw<InvalidOperationException>();
        Invoking(waived.Waive).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Participant_ComputedStateAndNotesCoverAllOutcomes()
    {
        var participant = new TestingParticipant { TenantId = null };
        participant.IsGlobal.Should().BeTrue();
        participant.TenantId = Guid.NewGuid();
        participant.IsGlobal.Should().BeFalse();
        participant.ParticipationDuration.Should().BeNull();

        participant.CompletedAt = SystemClock.UtcNow;
        participant.StartedAt = default;
        participant.ParticipationDuration.Should().BeNull();
        participant.StartedAt = participant.CompletedAt.Value.AddMinutes(-15);
        participant.ParticipationDuration.Should().Be(TimeSpan.FromMinutes(15));

        participant.AcknowledgeInstructions();
        participant.CanProvideFeedback.Should().BeFalse();
        participant.Start();
        participant.CanProvideFeedback.Should().BeTrue();
        participant.UpdateNotes("Details");
        participant.Notes.Should().Be("Details");
        participant.UpdateNotes(null);
        participant.Notes.Should().BeNull();
    }

    private static TestingSlotRegistration Registered(Guid userId) =>
        TestingSlotRegistration.Register(Guid.NewGuid(), Guid.NewGuid(), userId, null, Guid.NewGuid());

    private static TestingSlotRegistration Waitlisted(Guid userId) =>
        TestingSlotRegistration.Waitlist(Guid.NewGuid(), Guid.NewGuid(), userId, 1, null, Guid.NewGuid());

    private static TestingFeedbackObligation Obligation(
        Guid eventId, Guid slotId, Guid applicationId, Guid userId) =>
        TestingFeedbackObligation.Create(eventId, slotId, applicationId, userId, Guid.NewGuid());

    private static Action Invoking(Action action) => action;
}
