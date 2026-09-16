using System.Reflection;
using System.Runtime.ExceptionServices;
using FluentAssertions;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingEventLifecycleCoverageTests
{
    [Fact]
    public void Create_RejectsEveryInvalidScheduleInput()
    {
        var managerId = Guid.NewGuid();
        var open = SystemClock.UtcNow.AddDays(1);
        var close = open.AddDays(1);
        var start = close.AddDays(1);
        var end = start.AddHours(2);

        Invoking(() => Create(" ", managerId, open, close, start, end)).Should().Throw<ArgumentException>();
        Invoking(() => Create("Event", Guid.Empty, open, close, start, end)).Should().Throw<ArgumentException>();
        Invoking(() => Create("Event", managerId, open, open, start, end)).Should().Throw<ArgumentException>();
        Invoking(() => Create("Event", managerId, open, close, close.AddMinutes(-1), end)).Should().Throw<ArgumentException>();
        Invoking(() => Create("Event", managerId, open, close, start, start)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NormalizesOptionalTextAndRecurrenceMetadata()
    {
        var open = SystemClock.UtcNow.AddDays(1);
        var close = open.AddDays(1);
        var start = close.AddDays(1);
        var end = start.AddHours(2);
        var seriesId = Guid.NewGuid();

        var testingEvent = TestingEvent.Create(
            "  Event  ", TestingEventMode.Hybrid, Guid.NewGuid(), open, close, start, end,
            true, TestingEventApprovalMode.Committee, Guid.NewGuid(), "  Description  ",
            seriesId, 2, TestingEventRecurrenceFrequency.Weekly, 2, "Monday,Friday",
            end.AddMonths(2), 5, "  UTC  ");

        testingEvent.Name.Should().Be("Event");
        testingEvent.Description.Should().Be("Description");
        testingEvent.TimeZoneId.Should().Be("UTC");
        testingEvent.RecurrenceSeriesId.Should().Be(seriesId);
        testingEvent.RecurrenceOccurrence.Should().Be(2);
        testingEvent.RecurrenceFrequency.Should().Be(TestingEventRecurrenceFrequency.Weekly);
        testingEvent.RecurrenceInterval.Should().Be(2);
        testingEvent.RecurrenceDaysOfWeek.Should().Be("Monday,Friday");
        testingEvent.RecurrenceEndsAt.Should().Be(end.AddMonths(2));
        testingEvent.RecurrenceOccurrenceCount.Should().Be(5);

        Create("Event", Guid.NewGuid(), open, close, start, end, " ").Description.Should().BeNull();
    }

    [Fact]
    public void CreateAndUpdate_RejectInvalidTimeZoneIdentifiers()
    {
        var testingEvent = NewEvent();
        var schedule = Schedule();

        Invoking(() => CreateWithTimeZone(" ")).Should().Throw<ArgumentException>();
        Invoking(() => CreateWithTimeZone(new string('x', 101))).Should().Throw<ArgumentException>();
        Invoking(() => CreateWithTimeZone("Not/A-Time-Zone")).Should().Throw<ArgumentException>();
        Invoking(() => testingEvent.Update(
            "Updated", null, TestingEventMode.Online, TestingEventApprovalMode.ManagerOnly,
            schedule.Open, schedule.Close, schedule.Start, schedule.End, false, " "))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_AppliesValuesAndRejectsEveryInvalidStateOrSchedule()
    {
        var testingEvent = NewEvent();
        var schedule = Schedule(10);

        testingEvent.Update(
            "  Updated  ", "  Updated description  ", TestingEventMode.Hybrid,
            TestingEventApprovalMode.Committee, schedule.Open, schedule.Close,
            schedule.Start, schedule.End, false, "UTC");

        testingEvent.Name.Should().Be("Updated");
        testingEvent.Description.Should().Be("Updated description");
        testingEvent.Mode.Should().Be(TestingEventMode.Hybrid);
        testingEvent.ApprovalMode.Should().Be(TestingEventApprovalMode.Committee);
        testingEvent.RequiresFeedback.Should().BeFalse();
        Invoking(() => testingEvent.Update(" ", null, TestingEventMode.Online,
            TestingEventApprovalMode.ManagerOnly, schedule.Open, schedule.Close,
            schedule.Start, schedule.End, false)).Should().Throw<ArgumentException>();
        Invoking(() => testingEvent.Update("Event", null, TestingEventMode.Online,
            TestingEventApprovalMode.ManagerOnly, schedule.Open, schedule.Open,
            schedule.Start, schedule.End, false)).Should().Throw<ArgumentException>();
        Invoking(() => testingEvent.Update("Event", null, TestingEventMode.Online,
            TestingEventApprovalMode.ManagerOnly, schedule.Open, schedule.Close,
            schedule.Close.AddMinutes(-1), schedule.End, false)).Should().Throw<ArgumentException>();
        Invoking(() => testingEvent.Update("Event", null, TestingEventMode.Online,
            TestingEventApprovalMode.ManagerOnly, schedule.Open, schedule.Close,
            schedule.Start, schedule.Start, false)).Should().Throw<ArgumentException>();

        testingEvent.OpenConfiguredApplications();
        testingEvent.CloseApplications();
        testingEvent.Schedule();
        testingEvent.Activate();
        Invoking(() => testingEvent.Update("Event", null, TestingEventMode.Online,
            TestingEventApprovalMode.ManagerOnly, schedule.Open, schedule.Close,
            schedule.Start, schedule.End, false)).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Lifecycle_RejectsOutOfOrderAndTerminalTransitions()
    {
        var testingEvent = NewEvent();
        Invoking(testingEvent.CloseApplications).Should().Throw<InvalidOperationException>();
        Invoking(testingEvent.Schedule).Should().Throw<InvalidOperationException>();
        Invoking(testingEvent.Activate).Should().Throw<InvalidOperationException>();
        Invoking(testingEvent.Complete).Should().Throw<InvalidOperationException>();
        Invoking(() => testingEvent.Cancel(" ")).Should().Throw<ArgumentException>();

        testingEvent.OpenConfiguredApplications();
        Invoking(testingEvent.OpenApplications).Should().Throw<InvalidOperationException>();
        testingEvent.CloseApplications();
        testingEvent.Schedule();
        testingEvent.Activate();
        testingEvent.Complete();
        Invoking(() => testingEvent.Cancel("Too late")).Should().Throw<InvalidOperationException>();

        var cancelled = NewEvent();
        cancelled.Cancel("  Weather  ");
        cancelled.CancellationReason.Should().Be("Weather");
        cancelled.CancelledAt.Should().NotBeNull();
        Invoking(() => cancelled.Cancel("Again")).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reminders_NormalizeOverridesAndRemainIdempotent()
    {
        var testingEvent = NewEvent();

        testingEvent.SetReminderOverride([7, 1, 7, 0, 31, -1]);
        testingEvent.ReminderDaysBeforeOverride.Should().Be("1,7");
        testingEvent.SetReminderOverride([]);
        testingEvent.ReminderDaysBeforeOverride.Should().BeNull();
        testingEvent.SetReminderOverride(null);
        testingEvent.ReminderDaysBeforeOverride.Should().BeNull();

        testingEvent.MarkReminderSent(7);
        testingEvent.MarkReminderSent(1);
        testingEvent.MarkReminderSent(7);
        testingEvent.SentReminderDays.Should().Be("1,7");
        testingEvent.HasReminderBeenSent(7).Should().BeTrue();
        testingEvent.HasReminderBeenSent(3).Should().BeFalse();

        typeof(TestingEvent).GetProperty(nameof(TestingEvent.SentReminderDays))!
            .SetValue(testingEvent, "0, invalid, 3");
        testingEvent.HasReminderBeenSent(3).Should().BeTrue();
        testingEvent.HasReminderBeenSent(0).Should().BeFalse();
    }

    [Fact]
    public void ConfigureLearning_ValidatesIdentifiersStateAndFlags()
    {
        var testingEvent = NewEvent();
        var courseId = Guid.NewGuid();
        var cohortId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var requirement = TestingLearningCompletionRequirement.Attendance |
                          TestingLearningCompletionRequirement.FeedbackSubmitted;

        testingEvent.ConfigureLearning(courseId, cohortId, activityId, requirement);
        testingEvent.CourseId.Should().Be(courseId);
        testingEvent.CohortId.Should().Be(cohortId);
        testingEvent.LearningActivityId.Should().Be(activityId);
        testingEvent.LearningCompletionRequirement.Should().Be(requirement);

        Invoking(() => NewEvent().ConfigureLearning(Guid.Empty, null, activityId, requirement))
            .Should().Throw<ArgumentException>();
        Invoking(() => NewEvent().ConfigureLearning(courseId, null, Guid.Empty, requirement))
            .Should().Throw<ArgumentException>();
        Invoking(() => NewEvent().ConfigureLearning(courseId, null, activityId,
            TestingLearningCompletionRequirement.None)).Should().Throw<ArgumentOutOfRangeException>();
        Invoking(() => NewEvent().ConfigureLearning(courseId, null, activityId,
            (TestingLearningCompletionRequirement)128)).Should().Throw<ArgumentOutOfRangeException>();

        var active = NewEvent();
        active.OpenConfiguredApplications();
        active.CloseApplications();
        active.Schedule();
        active.Activate();
        Invoking(() => active.ConfigureLearning(courseId, null, activityId, requirement))
            .Should().Throw<InvalidOperationException>();
    }

    private static Action Invoking(Action action) => action;

    private static TestingEvent NewEvent()
    {
        var schedule = Schedule();
        return Create("Event", Guid.NewGuid(), schedule.Open, schedule.Close, schedule.Start, schedule.End);
    }

    private static TestingEvent Create(
        string name, Guid managerId, DateTime open, DateTime close, DateTime start, DateTime end,
        string? description = null) => TestingEvent.Create(
        name, TestingEventMode.Online, managerId, open, close, start, end, true,
        TestingEventApprovalMode.ManagerOnly, Guid.NewGuid(), description);

    private static TestingEvent CreateWithTimeZone(string timeZoneId)
    {
        var schedule = Schedule();
        return TestingEvent.Create(
            "Event", TestingEventMode.Online, Guid.NewGuid(), schedule.Open, schedule.Close,
            schedule.Start, schedule.End, true, TestingEventApprovalMode.ManagerOnly,
            Guid.NewGuid(), timeZoneId: timeZoneId);
    }

    private static (DateTime Open, DateTime Close, DateTime Start, DateTime End) Schedule(int days = 2)
    {
        var start = SystemClock.UtcNow.AddDays(days);
        return (start.AddDays(-2), start.AddDays(-1), start, start.AddHours(2));
    }
}

public sealed class TestingEventSlotCoverageTests
{
    [Fact]
    public void Create_RejectsInvalidIdentifiersScheduleCapacityAndRequiredModeDetails()
    {
        var start = SystemClock.UtcNow.AddDays(1);
        Func<Guid, TestingEventMode, DateTime, DateTime, int?, int?, string?, string?, string?, TestingEventSlot> create =
            (eventId, mode, startsAt, endsAt, testers, projects, campus, room, meeting) =>
                TestingEventSlot.Create(eventId, mode, startsAt, endsAt, testers, projects,
                    campus, room, meeting, Guid.NewGuid());

        Invoking(() => create(Guid.Empty, TestingEventMode.Hybrid, start, start.AddHours(1), null, null, null, null, null))
            .Should().Throw<ArgumentException>();
        Invoking(() => create(Guid.NewGuid(), TestingEventMode.Hybrid, start, start, null, null, null, null, null))
            .Should().Throw<ArgumentException>();
        Invoking(() => create(Guid.NewGuid(), TestingEventMode.Hybrid, start, start.AddHours(1), 0, null, null, null, null))
            .Should().Throw<ArgumentOutOfRangeException>();
        Invoking(() => create(Guid.NewGuid(), TestingEventMode.Hybrid, start, start.AddHours(1), null, 0, null, null, null))
            .Should().Throw<ArgumentOutOfRangeException>();
        Invoking(() => create(Guid.NewGuid(), TestingEventMode.InPerson, start, start.AddHours(1), null, null, "Campus", null, null))
            .Should().Throw<ArgumentException>();
        Invoking(() => create(Guid.NewGuid(), TestingEventMode.Online, start, start.AddHours(1), null, null, null, null, " "))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_AppliesNormalizedValuesAndRejectsEveryInvalidInput()
    {
        var start = SystemClock.UtcNow.AddDays(1);
        var slot = TestingEventSlot.Create(
            Guid.NewGuid(), TestingEventMode.Hybrid, start, start.AddHours(1), null, null,
            null, null, null, Guid.NewGuid());
        var locationId = Guid.NewGuid();

        slot.Update(TestingEventMode.InPerson, start.AddDays(1), start.AddDays(1).AddHours(2),
            10, 3, "  Campus  ", "  Room  ", " ", locationId);

        slot.Mode.Should().Be(TestingEventMode.InPerson);
        slot.MaxTesters.Should().Be(10);
        slot.MaxProjects.Should().Be(3);
        slot.CampusName.Should().Be("Campus");
        slot.RoomName.Should().Be("Room");
        slot.MeetingUrl.Should().BeNull();
        slot.LocationId.Should().Be(locationId);
        slot.IsTesterCapacityUnlimited.Should().BeFalse();
        slot.IsProjectCapacityUnlimited.Should().BeFalse();

        Invoking(() => slot.Update(TestingEventMode.Hybrid, start, start, null, null, null, null, null, null))
            .Should().Throw<ArgumentException>();
        Invoking(() => slot.Update(TestingEventMode.Hybrid, start, start.AddHours(1), 0, null, null, null, null, null))
            .Should().Throw<ArgumentOutOfRangeException>();
        Invoking(() => slot.Update(TestingEventMode.Hybrid, start, start.AddHours(1), null, 0, null, null, null, null))
            .Should().Throw<ArgumentOutOfRangeException>();
        Invoking(() => slot.Update(TestingEventMode.InPerson, start, start.AddHours(1), null, null, null, "Room", null, null))
            .Should().Throw<ArgumentException>();
        Invoking(() => slot.Update(TestingEventMode.Online, start, start.AddHours(1), null, null, null, null, null, null))
            .Should().Throw<ArgumentException>();

        slot.Update(TestingEventMode.Online, start, start.AddHours(1), null, null,
            null, null, "  https://example.test/meeting  ", null);
        slot.MeetingUrl.Should().Be("https://example.test/meeting");
        slot.CampusName.Should().BeNull();
        slot.RoomName.Should().BeNull();
    }

    private static Action Invoking(Action action) => action;
}

public sealed class TestingEventRecurrenceCoverageTests
{
    [Fact]
    public void Expand_CoversDailyMonthlyAndWeeklySchedules()
    {
        var start = new DateTime(2026, 1, 5, 15, 0, 0, DateTimeKind.Utc);

        Expand(start, new(TestingEventRecurrenceFrequency.Daily, 2, null, null, 3))
            .Should().Equal(start, start.AddDays(2), start.AddDays(4));
        Expand(start, new(TestingEventRecurrenceFrequency.Monthly, 1, null, start.AddMonths(2), null))
            .Should().Equal(start, start.AddMonths(1), start.AddMonths(2));
        Expand(start, new(TestingEventRecurrenceFrequency.Weekly, 1,
                [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Wednesday], null, 4))
            .Should().Equal(start, start.AddDays(2), start.AddDays(7), start.AddDays(9));
        Expand(start, new(TestingEventRecurrenceFrequency.Weekly, 2, null, null, 2))
            .Should().Equal(start, start.AddDays(14));
        Expand(start, null).Should().Equal(start);
    }

    [Fact]
    public void Expand_ValidatesAllRecurrenceBoundaries()
    {
        var start = new DateTime(2026, 1, 5, 15, 0, 0, DateTimeKind.Utc);

        Invoking(() => Expand(start, new((TestingEventRecurrenceFrequency)99, 1, null, null, 1)))
            .Should().Throw<ArgumentOutOfRangeException>();
        Invoking(() => Expand(start, new(TestingEventRecurrenceFrequency.Daily, 0, null, null, 1)))
            .Should().Throw<ArgumentOutOfRangeException>();
        Invoking(() => Expand(start, new(TestingEventRecurrenceFrequency.Daily, 53, null, null, 1)))
            .Should().Throw<ArgumentOutOfRangeException>();
        Invoking(() => Expand(start, new(TestingEventRecurrenceFrequency.Daily, 1, null, null, 0)))
            .Should().Throw<ArgumentOutOfRangeException>();
        Invoking(() => Expand(start, new(TestingEventRecurrenceFrequency.Daily, 1, null, null, 105)))
            .Should().Throw<ArgumentOutOfRangeException>();
        Invoking(() => Expand(start, new(TestingEventRecurrenceFrequency.Daily, 1, null, null, null)))
            .Should().Throw<ArgumentException>();
        Invoking(() => Expand(start, new(TestingEventRecurrenceFrequency.Daily, 1, null, start.AddDays(-1), null)))
            .Should().Throw<ArgumentException>();
        Invoking(() => Expand(start, new(TestingEventRecurrenceFrequency.Weekly, 1,
                [(DayOfWeek)99], null, 1)))
            .Should().Throw<ArgumentOutOfRangeException>();
        Invoking(() => Expand(start, new(TestingEventRecurrenceFrequency.Weekly, 1,
                [], null, 1)))
            .Should().Throw<ArgumentException>().WithMessage("*at least one day*");
        Invoking(() => Expand(start, new(TestingEventRecurrenceFrequency.Daily, 1, null,
                start.AddDays(200), null)))
            .Should().Throw<ArgumentException>().WithMessage("*104*");
        Invoking(() => Expand(start, new(TestingEventRecurrenceFrequency.Daily, 1, null, null, 1),
                "Not/A-Time-Zone"))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Expand_NormalizesLocalAndUnspecifiedDates()
    {
        var local = new DateTime(2026, 2, 2, 10, 0, 0, DateTimeKind.Local);
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);

        Expand(local, new(TestingEventRecurrenceFrequency.Daily, 1, null, null, 1))
            .Should().ContainSingle().Which.Kind.Should().Be(DateTimeKind.Utc);
        Expand(unspecified, new(TestingEventRecurrenceFrequency.Daily, 1, null, null, 1))
            .Should().ContainSingle().Which.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Expand_StopsWeeklySchedulesAtEndDateAndRejectsDstGaps()
    {
        var start = new DateTime(2026, 1, 5, 15, 0, 0, DateTimeKind.Utc);
        Expand(start, new(
                TestingEventRecurrenceFrequency.Weekly,
                1,
                [DayOfWeek.Monday, DayOfWeek.Wednesday],
                start.AddDays(3),
                null))
            .Should().Equal(start, start.AddDays(2));

        var beforeDstGap = new DateTime(2026, 3, 7, 7, 30, 0, DateTimeKind.Utc);
        Invoking(() => Expand(beforeDstGap, new(
                TestingEventRecurrenceFrequency.Daily, 1, null, null, 2),
                "America/New_York"))
            .Should().Throw<ArgumentException>().WithMessage("*does not exist*");
    }

    private static IReadOnlyList<DateTime> Expand(
        DateTime startsAt,
        TestingEventRecurrenceRequest? recurrence,
        string timeZoneId = "UTC")
    {
        var type = typeof(TestingEvent).Assembly.GetType(
            "GameGuild.TestingLab.TestingEventRecurrenceSchedule", throwOnError: true)!;
        var method = type.GetMethod("Expand", BindingFlags.Public | BindingFlags.Static)!;
        try
        {
            return (IReadOnlyList<DateTime>)method.Invoke(null, [startsAt, recurrence, timeZoneId])!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static Action Invoking(Action action) => action;
}
