using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Contracts;

public sealed class ProgramContentMappingCoverageTests
{
    [Fact]
    public void ToDto_PreservesUnterminatedImportMarker()
    {
        const string body = "<!-- gameguild-source:legacy";
        var content = new ProgramContent
        {
            Title = "Legacy lesson",
            Type = ProgramContentType.Lesson,
            Body = body
        };

        var dto = content.ToDto();

        dto.Body.Should().Be(body);
    }

    [Fact]
    public void ToDto_RemovesImportMarkerAndLeadingLineBreaks()
    {
        var content = new ProgramContent
        {
            Title = "Imported lesson",
            Type = ProgramContentType.Lesson,
            Body = "<!-- gameguild-source:legacy -->\r\n\n# Visible lesson"
        };

        var dto = content.ToDto();

        dto.Body.Should().Be("# Visible lesson");
    }

    [Fact]
    public void ToEntity_AppliesActivitySettings()
    {
        var settings = new DiscussionActivitySettings(
            AllowReplies: false,
            RequireThreadRoot: false,
            MinimumBodyLength: 5,
            MaximumBodyLength: 500);
        var dto = new CreateProgramContentDto
        {
            ProgramId = Guid.NewGuid(),
            Title = "Discussion",
            Type = ProgramContentType.Discussion,
            ActivitySettings = settings
        };

        var content = dto.ToEntity();

        content.GetActivitySettings().Should().BeEquivalentTo(settings);
    }

    [Fact]
    public void ApplyUpdates_UsesExplicitLessonFormat()
    {
        var content = new ProgramContent
        {
            Title = "Lesson",
            Type = ProgramContentType.Lesson,
            Body = "# Markdown",
            LessonFormat = LessonContentFormat.Markdown
        };
        var update = new UpdateProgramContentDto
        {
            Id = content.Id,
            LessonFormat = LessonContentFormat.RevealJs
        };

        content.ApplyUpdates(update);

        content.LessonFormat.Should().Be(LessonContentFormat.RevealJs);
    }
}
