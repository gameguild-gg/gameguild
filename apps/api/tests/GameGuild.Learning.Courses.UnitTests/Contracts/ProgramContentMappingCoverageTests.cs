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

    [Fact]
    public void ToDto_MapsStructuredContentAndLoadedRelationships()
    {
        var parent = new ProgramContent { Id = Guid.NewGuid(), Title = "Module" };
        var visibleChild = new ProgramContent { Id = Guid.NewGuid(), Title = "Visible child" };
        var deletedChild = new ProgramContent { Id = Guid.NewGuid(), Title = "Deleted child", Version = 1 };
        deletedChild.SoftDelete();
        var content = new ProgramContent
        {
            Id = Guid.NewGuid(),
            ProgramId = Guid.NewGuid(),
            Title = "Structured lesson",
            Description = "A description",
            Type = ProgramContentType.Lesson,
            JsonBody = "{\"root\":{}}",
            LessonFormat = LessonContentFormat.Lexical,
            Program = new Program { Title = "Course" },
            Parent = parent,
            Children = [visibleChild, deletedChild],
        };

        var dto = content.ToDto();

        dto.Description.Should().Be("A description");
        dto.JsonBody.Should().NotBeNull();
        dto.LessonFormat.Should().Be(LessonContentFormat.Lexical);
        dto.ProgramTitle.Should().Be("Course");
        dto.ParentTitle.Should().Be("Module");
        dto.ChildrenCount.Should().Be(1);
        dto.Children.Should().ContainSingle().Which.Id.Should().Be(visibleChild.Id);
    }

    [Fact]
    public void ToDto_WhenOptionalRelationshipsAreNotLoaded_UsesEmptyValues()
    {
        var content = new ProgramContent
        {
            Title = "Assignment",
            Type = ProgramContentType.Assignment,
            Children = null!,
        };

        var dto = content.ToDto();

        dto.LessonFormat.Should().BeNull();
        dto.ChildrenCount.Should().Be(0);
        dto.Children.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_WhenLessonFormatIsMissing_InfersItFromBody()
    {
        var content = new ProgramContent
        {
            Title = "Imported HTML lesson",
            Type = ProgramContentType.Lesson,
            Body = "<article>Lesson</article>",
            LessonFormat = null,
        };

        var dto = content.ToDto();

        dto.LessonFormat.Should().Be(LessonContentFormat.Html);
    }

    [Fact]
    public void ToEntity_PreservesExplicitAuthoringMetadata()
    {
        var json = System.Text.Json.JsonDocument.Parse("{\"root\":{}}").RootElement.Clone();
        var dto = new CreateProgramContentDto
        {
            ProgramId = Guid.NewGuid(),
            Title = "Explicit lesson",
            Slug = "stable-slug",
            Description = "Description",
            Type = ProgramContentType.Lesson,
            Body = "ignored by structured content",
            JsonBody = json,
            LessonFormat = LessonContentFormat.Lexical,
            EstimatedMinutesSource = EstimatedMinutesSource.Manual,
        };

        var content = dto.ToEntity();

        content.Slug.Should().Be("stable-slug");
        content.JsonBody.Should().NotBeNull();
        content.Body.Should().BeNull();
        content.LessonFormat.Should().Be(LessonContentFormat.Lexical);
        content.EstimatedMinutesSource.Should().Be(EstimatedMinutesSource.Manual);
    }

    [Fact]
    public void ApplyUpdates_StructuredBodyWinsAndManualEstimateIsRecorded()
    {
        var content = new ProgramContent
        {
            Title = "Lesson",
            Type = ProgramContentType.Lesson,
            Body = "# Old",
            LessonFormat = LessonContentFormat.Markdown,
        };
        var json = System.Text.Json.JsonDocument.Parse("{\"root\":{}}").RootElement.Clone();
        var update = new UpdateProgramContentDto
        {
            Id = content.Id,
            JsonBody = json,
            LessonFormat = LessonContentFormat.Lexical,
            EstimatedMinutes = 12,
        };

        content.ApplyUpdates(update);

        content.JsonBody.Should().NotBeNull();
        content.Body.Should().BeNull();
        content.EstimatedMinutes.Should().Be(12);
        content.EstimatedMinutesSource.Should().Be(EstimatedMinutesSource.Manual);
    }

    [Fact]
    public void ApplyUpdates_WhenContentBecomesLesson_InfersFormatFromBody()
    {
        var content = new ProgramContent
        {
            Title = "Assignment",
            Type = ProgramContentType.Assignment,
            Body = "old",
            LessonFormat = null,
        };
        var update = new UpdateProgramContentDto
        {
            Id = content.Id,
            Type = ProgramContentType.Lesson,
            Body = "<p>Lesson</p>",
        };

        content.ApplyUpdates(update);

        content.Type.Should().Be(ProgramContentType.Lesson);
        content.LessonFormat.Should().Be(LessonContentFormat.Html);
    }

    [Fact]
    public void ApplyUpdates_WhenAutoEstimateIsRequested_PreservesAutomaticSource()
    {
        var content = new ProgramContent
        {
            Title = "Lesson",
            Type = ProgramContentType.Lesson,
            Body = "A short lesson",
            EstimatedMinutes = 10,
            EstimatedMinutesSource = EstimatedMinutesSource.Manual,
        };

        content.ApplyUpdates(new UpdateProgramContentDto
        {
            Id = content.Id,
            EstimatedMinutesSource = EstimatedMinutesSource.Auto,
        });

        content.EstimatedMinutesSource.Should().Be(EstimatedMinutesSource.Auto);
    }

    [Fact]
    public void ApplyUpdates_MapsAllMutableScalarFieldsOnNonLessonContent()
    {
        var content = new ProgramContent
        {
            Title = "Assignment",
            Type = ProgramContentType.Assignment,
            Slug = "old-slug",
            Description = "Old description",
            SortOrder = 1,
            IsRequired = true,
            Visibility = Visibility.Private,
        };

        content.ApplyUpdates(new UpdateProgramContentDto
        {
            Id = content.Id,
            Slug = "new-slug",
            Description = "New description",
            SortOrder = 7,
            IsRequired = false,
            Visibility = Visibility.Public,
        });

        content.Slug.Should().Be("new-slug");
        content.Description.Should().Be("New description");
        content.SortOrder.Should().Be(7);
        content.IsRequired.Should().BeFalse();
        content.Visibility.Should().Be(Visibility.Public);
    }

    [Fact]
    public void ApplyUpdates_MapsActivitySettingsForMatchingActivityType()
    {
        var content = new ProgramContent
        {
            Title = "Discussion",
            Type = ProgramContentType.Discussion,
        };
        var settings = new DiscussionActivitySettings(
            AllowReplies: true,
            RequireThreadRoot: true,
            MinimumBodyLength: 5,
            MaximumBodyLength: 500);

        content.ApplyUpdates(new UpdateProgramContentDto
        {
            Id = content.Id,
            ActivitySettings = settings,
        });

        content.GetActivitySettings().Should().Be(settings);
    }
}
