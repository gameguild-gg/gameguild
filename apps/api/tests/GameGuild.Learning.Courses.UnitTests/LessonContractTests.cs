using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests;

public sealed class LessonContractTests
{
    [Fact]
    public void LessonContentFormat_ShouldKeepExplicitPersistedValues()
    {
        ((int)LessonContentFormat.Markdown).Should().Be(0);
        ((int)LessonContentFormat.Lexical).Should().Be(1);
        ((int)LessonContentFormat.RevealJs).Should().Be(2);
        ((int)LessonContentFormat.Video).Should().Be(3);
    }

    [Fact]
    public void NewLesson_ShouldDefaultToMarkdownAndRemainNonGraded()
    {
        var lesson = new ProgramContent();

        lesson.Type.Should().Be(ProgramContentType.Lesson);
        lesson.LessonFormat.Should().Be(LessonContentFormat.Markdown);
        lesson.GradingMethod.Should().Be(GradingMethod.None);
        lesson.MaxPoints.Should().BeNull();
    }

    [Fact]
    public void SetGrading_WhenContentIsLesson_ShouldRejectGrading()
    {
        var lesson = new ProgramContent { Type = ProgramContentType.Lesson };

        var action = () => lesson.SetGrading(GradingMethod.Instructor, 100);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Lessons cannot be graded*");
    }

    [Fact]
    public void ToEntity_WhenLessonContainsLexicalState_ShouldInferLexicalFormat()
    {
        var dto = CreateLessonDto("""{"root":{"type":"root","children":[]}}""");

        var lesson = dto.ToEntity();

        lesson.LessonFormat.Should().Be(LessonContentFormat.Lexical);
        lesson.GradingMethod.Should().Be(GradingMethod.None);
        lesson.MaxPoints.Should().BeNull();
    }

    [Theory]
    [InlineData(LessonContentFormat.Markdown)]
    [InlineData(LessonContentFormat.RevealJs)]
    [InlineData(LessonContentFormat.Video)]
    public void ToEntity_WhenLessonFormatIsExplicit_ShouldPreserveIt(LessonContentFormat format)
    {
        var dto = CreateLessonDto("lesson body");
        dto.LessonFormat = format;

        var lesson = dto.ToEntity();

        lesson.LessonFormat.Should().Be(format);
    }

    [Fact]
    public void ToEntity_WhenLessonReceivesGradingFields_ShouldNormalizeThemAway()
    {
        var dto = CreateLessonDto("lesson body");
        dto.GradingMethod = GradingMethod.Instructor;
        dto.MaxPoints = 50;

        var lesson = dto.ToEntity();

        lesson.GradingMethod.Should().Be(GradingMethod.None);
        lesson.MaxPoints.Should().BeNull();
    }

    [Fact]
    public void ApplyUpdates_WhenAssignmentBecomesLesson_ShouldClearGradingAndInferFormat()
    {
        var content = new ProgramContent
        {
            Type = ProgramContentType.Assignment,
            GradingMethod = GradingMethod.Instructor,
            MaxPoints = 100,
            LessonFormat = null,
        };
        var dto = new UpdateProgramContentDto
        {
            Id = content.Id,
            Type = ProgramContentType.Lesson,
            Body = """{"root":{"type":"root","children":[]}}""",
        };

        content.ApplyUpdates(dto);

        content.Type.Should().Be(ProgramContentType.Lesson);
        content.LessonFormat.Should().Be(LessonContentFormat.Lexical);
        content.GradingMethod.Should().Be(GradingMethod.None);
        content.MaxPoints.Should().BeNull();
    }

    [Theory]
    [InlineData(LessonContentFormat.RevealJs)]
    [InlineData(LessonContentFormat.Video)]
    public void ApplyUpdates_WhenLessonBodyChangesWithoutFormat_ShouldPreserveExplicitFormat(
        LessonContentFormat format)
    {
        var content = new ProgramContent
        {
            Type = ProgramContentType.Lesson,
            LessonFormat = format,
            Body = "old body",
        };
        var dto = new UpdateProgramContentDto
        {
            Id = content.Id,
            Body = "updated body",
        };

        content.ApplyUpdates(dto);

        content.LessonFormat.Should().Be(format);
    }

    [Fact]
    public void ToEntity_WhenContentIsAssignment_ShouldKeepGradingAndIgnoreLessonFormat()
    {
        var dto = new CreateProgramContentDto
        {
            ProgramId = Guid.NewGuid(),
            Title = "Coding assignment",
            Type = ProgramContentType.Assignment,
            Body = "{}",
            LessonFormat = LessonContentFormat.Video,
            GradingMethod = GradingMethod.AutomatedTests,
            MaxPoints = 100,
        };

        var assignment = dto.ToEntity();

        assignment.LessonFormat.Should().BeNull();
        assignment.GradingMethod.Should().Be(GradingMethod.AutomatedTests);
        assignment.MaxPoints.Should().Be(100);
    }

    [Fact]
    public void NormalizeLearningContract_WhenSeederProvidesFormatForAssignment_ShouldClearIt()
    {
        var assignment = new ProgramContent
        {
            Type = ProgramContentType.Assignment,
            LessonFormat = LessonContentFormat.Markdown,
        };

        assignment.NormalizeLearningContract();

        assignment.LessonFormat.Should().BeNull();
    }

    [Fact]
    public void NormalizeLearningContract_WhenLessonFormatIsUnknown_ShouldRejectIt()
    {
        var lesson = new ProgramContent
        {
            Type = ProgramContentType.Lesson,
            LessonFormat = (LessonContentFormat)999,
        };

        var action = lesson.NormalizeLearningContract;

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(ProgramContent.LessonFormat));
    }

    [Fact]
    public void DatabaseContract_ShouldRestrictPersistedLessonFormatValues()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());
        var entity = modelBuilder.Entity<ProgramContent>();

        new ProgramContentConfiguration().Configure(entity);

        var constraint = entity.Metadata.GetCheckConstraints()
            .Single(item => item.Name == "CK_program_contents_LessonFormat");
        constraint.Sql.Should().Contain("\"LessonFormat\" IN (0, 1, 2, 3, 4, 5)");
    }

    private static CreateProgramContentDto CreateLessonDto(string body) =>
        new()
        {
            ProgramId = Guid.NewGuid(),
            Title = "Lesson",
            Type = ProgramContentType.Lesson,
            Body = body,
        };
}
