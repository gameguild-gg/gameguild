using System.Text.Json;
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
    public void NewLesson_ShouldDefaultToMarkdown()
    {
        var lesson = new ProgramContent();

        lesson.Type.Should().Be(ProgramContentType.Lesson);
        lesson.LessonFormat.Should().Be(LessonContentFormat.Markdown);
    }

    [Fact]
    public void ToEntity_WhenLessonContainsLexicalState_ShouldInferLexicalFormat()
    {
        var dto = CreateLessonDto("""{"root":{"type":"root","children":[]}}""");

        var lesson = dto.ToEntity();

        lesson.LessonFormat.Should().Be(LessonContentFormat.Lexical);
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
    public void ApplyUpdates_WhenAssignmentBecomesLesson_ShouldInferFormat()
    {
        var content = new ProgramContent
        {
            Type = ProgramContentType.Assignment,
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
    public void ToEntity_WhenContentIsAssignment_ShouldIgnoreLessonFormat()
    {
        var dto = new CreateProgramContentDto
        {
            ProgramId = Guid.NewGuid(),
            Title = "Coding assignment",
            Type = ProgramContentType.Assignment,
            Body = "{}",
            LessonFormat = LessonContentFormat.Video,
        };

        var assignment = dto.ToEntity();

        assignment.LessonFormat.Should().BeNull();
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

    [Fact]
    public void NormalizeLearningContract_RoutesLexicalLessonToJsonBody()
    {
        var lesson = new ProgramContent
        {
            Type = ProgramContentType.Lesson,
            LessonFormat = LessonContentFormat.Lexical,
            JsonBody = """{"root":{"type":"root","children":[]}}""",
            Body = "stale text body",
        };

        lesson.NormalizeLearningContract();

        lesson.Body.Should().BeNull();
        lesson.JsonBody.Should().Be("""{"root":{"type":"root","children":[]}}""");
    }

    [Fact]
    public void NormalizeLearningContract_RoutesMarkdownLessonToBody()
    {
        var lesson = new ProgramContent
        {
            Type = ProgramContentType.Lesson,
            LessonFormat = LessonContentFormat.Markdown,
            Body = "# hi",
            JsonBody = """{"root":{"stale":true}}""",
        };

        lesson.NormalizeLearningContract();

        lesson.JsonBody.Should().BeNull();
        lesson.Body.Should().Be("# hi");
    }

    [Fact]
    public void NormalizeLearningContract_QuestionnaireUsesJsonBody()
    {
        var content = new ProgramContent
        {
            Type = ProgramContentType.Questionnaire,
            JsonBody = """{"questions":[]}""",
            Body = null,
        };

        content.NormalizeLearningContract();

        content.Body.Should().BeNull();
        content.JsonBody.Should().Be("""{"questions":[]}""");
    }

    [Fact]
    public void NormalizeLearningContract_AssignmentClearsJsonBodyForTextRouting()
    {
        var content = new ProgramContent
        {
            Type = ProgramContentType.Assignment,
            Body = "instructions",
            JsonBody = """{"stale":true}""",
        };

        content.NormalizeLearningContract();

        content.JsonBody.Should().BeNull();
        content.Body.Should().Be("instructions");
    }

    [Fact]
    public void ApplyUpdates_WhenJsonBodySent_ClearsBodyForLexicalLesson()
    {
        var content = new ProgramContent
        {
            Type = ProgramContentType.Lesson,
            LessonFormat = LessonContentFormat.Lexical,
            Body = "old text",
        };
        var dto = new UpdateProgramContentDto
        {
            Id = content.Id,
            JsonBody = JsonDocument.Parse("""{"root":{"type":"root","children":[]}}""").RootElement.Clone(),
        };

        content.ApplyUpdates(dto);

        content.Body.Should().BeNull();
        content.JsonBody.Should().NotBeNull();
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
