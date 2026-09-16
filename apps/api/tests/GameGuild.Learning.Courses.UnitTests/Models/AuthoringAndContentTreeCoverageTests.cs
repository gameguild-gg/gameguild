using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Models;

public sealed class AuthoringAndContentTreeCoverageTests
{
    [Fact]
    public void AuthoringPayloadFrom_MapsAbsentAndStructuredJsonBodies()
    {
        var plain = new ProgramContent
        {
            Title = "Plain lesson",
            Slug = "plain-lesson",
            Type = ProgramContentType.Lesson,
            JsonBody = null,
        };
        var structured = new ProgramContent
        {
            Title = "Structured lesson",
            Slug = "structured-lesson",
            Type = ProgramContentType.Lesson,
            JsonBody = "{\"root\":{}}",
            LessonFormat = LessonContentFormat.Lexical,
        };

        AuthoringContentPayload.From(plain).JsonBody.Should().BeNull();
        AuthoringContentPayload.From(structured).JsonBody.Should().NotBeNull();
    }

    [Fact]
    public void ContentTree_CycleTerminatesAndReturnsEachContentOnce()
    {
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var contents = new[]
        {
            new ProgramContent { Id = rootId, ParentId = childId, Title = "Root" },
            new ProgramContent { Id = childId, ParentId = rootId, Title = "Child" },
        };

        var ids = ProgramContentTree.GetIds(rootId, contents);

        ids.Should().BeEquivalentTo([rootId, childId]);
    }
}
