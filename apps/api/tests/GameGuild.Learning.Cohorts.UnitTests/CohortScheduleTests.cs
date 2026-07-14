using FluentAssertions;
using GameGuild.Learning.Cohorts;
using Xunit;

namespace GameGuild.Learning.Cohorts.UnitTests;

public class CohortScheduleEnumTests
{
    [Theory]
    [InlineData(CohortPacingMode.OneModulePerWeek, 0)]
    [InlineData(CohortPacingMode.OneLessonPerMeeting, 1)]
    [InlineData(CohortPacingMode.FixedLessonsPerWeek, 2)]
    [InlineData(CohortPacingMode.Manual, 3)]
    public void PacingMode_HasStableValues(CohortPacingMode value, int expected) =>
        ((int)value).Should().Be(expected);

    [Theory]
    [InlineData(CohortReleasePolicy.Weekly, 0)]
    [InlineData(CohortReleasePolicy.BeforeMeeting, 1)]
    [InlineData(CohortReleasePolicy.Manual, 2)]
    [InlineData(CohortReleasePolicy.Immediately, 3)]
    public void ReleasePolicy_HasStableValues(CohortReleasePolicy value, int expected) =>
        ((int)value).Should().Be(expected);

    [Theory]
    [InlineData(CohortScheduleItemType.ContentRelease, 0)]
    [InlineData(CohortScheduleItemType.LiveSession, 1)]
    [InlineData(CohortScheduleItemType.AssessmentWindow, 2)]
    [InlineData(CohortScheduleItemType.Milestone, 3)]
    public void ScheduleItemType_HasStableValues(CohortScheduleItemType value, int expected) =>
        ((int)value).Should().Be(expected);

    [Theory]
    [InlineData(CohortScheduleItemStatus.Draft, 0)]
    [InlineData(CohortScheduleItemStatus.Scheduled, 1)]
    [InlineData(CohortScheduleItemStatus.Published, 2)]
    [InlineData(CohortScheduleItemStatus.Completed, 3)]
    [InlineData(CohortScheduleItemStatus.Cancelled, 4)]
    public void ScheduleItemStatus_HasStableValues(CohortScheduleItemStatus value, int expected) =>
        ((int)value).Should().Be(expected);

    [Theory]
    [InlineData(CohortVisibilityOverride.Inherited, 0)]
    [InlineData(CohortVisibilityOverride.Hidden, 1)]
    [InlineData(CohortVisibilityOverride.Visible, 2)]
    public void VisibilityOverride_HasStableValues(CohortVisibilityOverride value, int expected) =>
        ((int)value).Should().Be(expected);

    [Theory]
    [InlineData(ScheduleShiftScope.Single, 0)]
    [InlineData(ScheduleShiftScope.Following, 1)]
    public void ShiftScope_HasStableValues(ScheduleShiftScope value, int expected) =>
        ((int)value).Should().Be(expected);

    [Theory]
    [InlineData(ScheduleConflictSeverity.Advisory, 0)]
    [InlineData(ScheduleConflictSeverity.Blocking, 1)]
    public void ConflictSeverity_HasStableValues(ScheduleConflictSeverity value, int expected) =>
        ((int)value).Should().Be(expected);
}

public class CohortScheduleTests
{
    [Fact]
    public void Create_WithValidRules_SetsSchedulePolicy()
    {
        var cohortId = Guid.NewGuid();

        var schedule = CohortSchedule.Create(
            cohortId,
            "UTC",
            [DayOfWeek.Monday, DayOfWeek.Wednesday],
            new TimeOnly(9, 30),
            90,
            CohortPacingMode.OneLessonPerMeeting,
            1,
            CohortReleasePolicy.BeforeMeeting);

        schedule.CohortId.Should().Be(cohortId);
        schedule.TimezoneId.Should().Be("UTC");
        schedule.MeetingDays.Should().Equal(DayOfWeek.Monday, DayOfWeek.Wednesday);
        schedule.MeetingStartTime.Should().Be(new TimeOnly(9, 30));
        schedule.MeetingDurationMinutes.Should().Be(90);
        schedule.PacingMode.Should().Be(CohortPacingMode.OneLessonPerMeeting);
        schedule.UnitsPerPeriod.Should().Be(1);
        schedule.ReleasePolicy.Should().Be(CohortReleasePolicy.BeforeMeeting);
    }

    [Fact]
    public void Create_WithInvalidTimezone_ThrowsArgumentException()
    {
        var act = () => CreateSchedule(timezoneId: "Not/A/Timezone");

        act.Should().Throw<ArgumentException>().WithParameterName("timezoneId");
    }

    [Fact]
    public void Create_WithoutMeetingDays_ThrowsArgumentException()
    {
        var act = () => CreateSchedule(meetingDays: []);

        act.Should().Throw<ArgumentException>().WithParameterName("meetingDays");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveMeetingDuration_ThrowsArgumentOutOfRangeException(int duration)
    {
        var act = () => CreateSchedule(meetingDurationMinutes: duration);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("meetingDurationMinutes");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveUnitsPerPeriod_ThrowsArgumentOutOfRangeException(int units)
    {
        var act = () => CreateSchedule(unitsPerPeriod: units);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("unitsPerPeriod");
    }

    [Fact]
    public void UpdateRules_ReplacesCadenceWithoutChangingCohort()
    {
        var schedule = CreateSchedule();
        var cohortId = schedule.CohortId;

        schedule.UpdateRules(
            "UTC",
            [DayOfWeek.Tuesday, DayOfWeek.Thursday],
            new TimeOnly(19, 0),
            120,
            CohortPacingMode.FixedLessonsPerWeek,
            3,
            CohortReleasePolicy.Weekly);

        schedule.CohortId.Should().Be(cohortId);
        schedule.MeetingDays.Should().Equal(DayOfWeek.Tuesday, DayOfWeek.Thursday);
        schedule.MeetingStartTime.Should().Be(new TimeOnly(19, 0));
        schedule.MeetingDurationMinutes.Should().Be(120);
        schedule.PacingMode.Should().Be(CohortPacingMode.FixedLessonsPerWeek);
        schedule.UnitsPerPeriod.Should().Be(3);
        schedule.ReleasePolicy.Should().Be(CohortReleasePolicy.Weekly);
    }

    private static CohortSchedule CreateSchedule(
        string timezoneId = "UTC",
        IReadOnlyCollection<DayOfWeek>? meetingDays = null,
        int meetingDurationMinutes = 90,
        int unitsPerPeriod = 1) =>
        CohortSchedule.Create(
            Guid.NewGuid(),
            timezoneId,
            meetingDays ?? [DayOfWeek.Monday],
            new TimeOnly(9, 0),
            meetingDurationMinutes,
            CohortPacingMode.OneModulePerWeek,
            unitsPerPeriod,
            CohortReleasePolicy.Weekly);
}
