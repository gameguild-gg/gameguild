using FluentAssertions;
using GameGuild.Learning.Courses;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests;

public class ProgramContentEstimatedTimeTests
{
    [Fact]
    public void Recalculate_AutoSource_OverwritesFromBody()
    {
        var content = new ProgramContent
        {
            Title = "T",
            Type = ProgramContentType.Lesson,
            LessonFormat = LessonContentFormat.Markdown,
            Body = string.Join(" ", Enumerable.Repeat("word", 400)),
        };

        content.NormalizeLearningContract();

        content.EstimatedMinutes.Should().Be(2);
        content.EstimatedMinutesSource.Should().Be(EstimatedMinutesSource.Auto);
    }

    [Fact]
    public void Recalculate_ManualSource_PreservesValue()
    {
        var content = new ProgramContent
        {
            Title = "T",
            Type = ProgramContentType.Lesson,
            LessonFormat = LessonContentFormat.Markdown,
            Body = string.Join(" ", Enumerable.Repeat("word", 400)),
            EstimatedMinutes = 30,
            EstimatedMinutesSource = EstimatedMinutesSource.Manual,
        };

        content.NormalizeLearningContract();

        content.EstimatedMinutes.Should().Be(30);
        content.EstimatedMinutesSource.Should().Be(EstimatedMinutesSource.Manual);
    }

    [Fact]
    public void UpdateEstimatedTime_SetsManualSource()
    {
        var content = new ProgramContent();

        content.UpdateEstimatedTime(30);

        content.EstimatedMinutes.Should().Be(30);
        content.EstimatedMinutesSource.Should().Be(EstimatedMinutesSource.Manual);
    }

    [Fact]
    public void Recalculate_VideoLesson_LeavesValueUntouched()
    {
        var content = new ProgramContent
        {
            Title = "T",
            Type = ProgramContentType.Lesson,
            LessonFormat = LessonContentFormat.Video,
            Body = "https://youtube.com/watch?v=x",
            EstimatedMinutes = 45,
        };

        content.NormalizeLearningContract();

        content.EstimatedMinutes.Should().Be(45);
    }
}
