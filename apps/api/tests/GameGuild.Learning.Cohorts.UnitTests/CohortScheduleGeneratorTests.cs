using FluentAssertions;
using GameGuild.Learning.Cohorts;
using GameGuild.Learning.Courses;
using Xunit;

namespace GameGuild.Learning.Cohorts.UnitTests;

public class CohortScheduleGeneratorTests
{
    private readonly CohortScheduleGenerator _generator = new();

    [Fact]
    public void OneModulePerWeek_ReleasesModulesSevenDaysApart()
    {
        var preview = _generator.Generate(Request(
            content: Modules(3),
            pacingMode: CohortPacingMode.OneModulePerWeek));

        LocalReleaseDates(preview).Should().Equal(
            new DateOnly(2026, 8, 12),
            new DateOnly(2026, 8, 19),
            new DateOnly(2026, 8, 26));
    }

    [Fact]
    public void OneModulePerWeek_ReleasesChildLessonsWithTheirModule()
    {
        var moduleId = Guid.NewGuid();
        var module = new CanonicalScheduleContent(
            moduleId,
            null,
            null,
            "Foundations",
            ProgramContentType.Module,
            0,
            60);
        var lessons = Enumerable.Range(0, 2)
            .Select(index => new CanonicalScheduleContent(
                Guid.NewGuid(),
                null,
                moduleId,
                $"Lesson {index + 1}",
                ProgramContentType.Lesson,
                index,
                45))
            .ToArray();

        var preview = _generator.Generate(Request(
            content: [module, .. lessons],
            pacingMode: CohortPacingMode.OneModulePerWeek));

        preview.Items
            .Where(item => item.ProgramContentId.HasValue)
            .Should().HaveCount(3)
            .And.OnlyContain(item =>
                DateOnly.FromDateTime(item.AvailableFrom!.Value) == new DateOnly(2026, 8, 12));
    }

    [Fact]
    public void OneLessonPerMeeting_UsesEachMeetingDate()
    {
        var preview = _generator.Generate(Request(
            content: Lessons(3),
            pacingMode: CohortPacingMode.OneLessonPerMeeting,
            meetingDays: [DayOfWeek.Monday, DayOfWeek.Wednesday]));

        LocalReleaseDates(preview).Should().Equal(
            new DateOnly(2026, 8, 12),
            new DateOnly(2026, 8, 17),
            new DateOnly(2026, 8, 19));
    }

    [Fact]
    public void FixedLessonsPerWeek_RespectsUnitCount()
    {
        var preview = _generator.Generate(Request(
            content: Lessons(3),
            pacingMode: CohortPacingMode.FixedLessonsPerWeek,
            unitsPerPeriod: 2));

        LocalReleaseDates(preview).Should().Equal(
            new DateOnly(2026, 8, 12),
            new DateOnly(2026, 8, 12),
            new DateOnly(2026, 8, 19));
    }

    [Fact]
    public void ManualMode_ReturnsNoGeneratedItems()
    {
        var preview = _generator.Generate(Request(
            content: Lessons(3),
            pacingMode: CohortPacingMode.Manual));

        preview.Items.Should().BeEmpty();
        preview.CalculatedEndDate.Should().Be(new DateOnly(2026, 8, 12));
    }

    [Fact]
    public void SkippedDate_MovesToNextMeetingDay()
    {
        var preview = _generator.Generate(Request(
            content: Lessons(1),
            pacingMode: CohortPacingMode.OneLessonPerMeeting,
            meetingDays: [DayOfWeek.Monday, DayOfWeek.Wednesday],
            skippedDates: [new DateOnly(2026, 8, 12)]));

        LocalReleaseDates(preview).Should().Equal(new DateOnly(2026, 8, 17));
    }

    [Fact]
    public void DstBoundary_PreservesLocalStartTime()
    {
        const string timezoneId = "America/New_York";
        var preview = _generator.Generate(Request(
            firstDate: new DateOnly(2026, 3, 1),
            endDate: new DateOnly(2026, 3, 15),
            timezoneId: timezoneId,
            content: Lessons(2),
            pacingMode: CohortPacingMode.OneLessonPerMeeting,
            meetingDays: [DayOfWeek.Sunday],
            meetingStartTime: new TimeOnly(9, 0)));

        var timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        preview.Items
            .Where(item => item.Type == CohortScheduleItemType.LiveSession)
            .Take(2)
            .Select(item => TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(item.StartsAt!.Value, timezone)))
            .Should()
            .OnlyContain(time => time == new TimeOnly(9, 0));
    }

    [Fact]
    public void AssessmentDueDate_UsesConfiguredOffset()
    {
        var assessmentId = Guid.NewGuid();
        var assessment = new CanonicalScheduleContent(
            Guid.NewGuid(),
            assessmentId,
            null,
            "Week 1 quiz",
            ProgramContentType.Questionnaire,
            0,
            30);

        var preview = _generator.Generate(Request(
            content: [assessment],
            pacingMode: CohortPacingMode.OneModulePerWeek,
            assessmentDueOffsetDays: 5));

        var item = preview.Items.Single(candidate => candidate.AssessmentId == assessmentId);
        DateOnly.FromDateTime(item.DueAt!.Value).Should().Be(new DateOnly(2026, 8, 17));
    }

    private static CohortScheduleGenerationRequest Request(
        IReadOnlyCollection<CanonicalScheduleContent> content,
        CohortPacingMode pacingMode,
        DateOnly? firstDate = null,
        DateOnly? endDate = null,
        string timezoneId = "UTC",
        IReadOnlyCollection<DayOfWeek>? meetingDays = null,
        TimeOnly? meetingStartTime = null,
        int unitsPerPeriod = 1,
        IReadOnlyCollection<DateOnly>? skippedDates = null,
        int assessmentDueOffsetDays = 7) =>
        new(
            Guid.NewGuid(),
            firstDate ?? new DateOnly(2026, 8, 12),
            endDate ?? new DateOnly(2026, 12, 18),
            timezoneId,
            meetingDays ?? [DayOfWeek.Wednesday],
            meetingStartTime ?? new TimeOnly(9, 0),
            90,
            pacingMode,
            unitsPerPeriod,
            CohortReleasePolicy.Weekly,
            skippedDates ?? [],
            content,
            assessmentDueOffsetDays);

    private static IReadOnlyCollection<CanonicalScheduleContent> Modules(int count) =>
        Enumerable.Range(0, count)
            .Select(index => new CanonicalScheduleContent(
                Guid.NewGuid(),
                null,
                null,
                $"Module {index + 1}",
                ProgramContentType.Module,
                index,
                60))
            .ToArray();

    private static IReadOnlyCollection<CanonicalScheduleContent> Lessons(int count) =>
        Enumerable.Range(0, count)
            .Select(index => new CanonicalScheduleContent(
                Guid.NewGuid(),
                null,
                null,
                $"Lesson {index + 1}",
                ProgramContentType.Lesson,
                index,
                45))
            .ToArray();

    private static IEnumerable<DateOnly> LocalReleaseDates(CohortSchedulePreview preview) =>
        preview.Items
            .Where(item => item.ProgramContentId.HasValue)
            .OrderBy(item => item.SortOrder)
            .Select(item => DateOnly.FromDateTime(item.AvailableFrom!.Value));
}
