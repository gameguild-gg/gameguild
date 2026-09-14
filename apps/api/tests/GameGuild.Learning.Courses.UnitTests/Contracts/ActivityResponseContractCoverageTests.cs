using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Contracts;

public sealed class ActivityResponseContractCoverageTests
{
    [Fact]
    public void Parse_RejectsNonActivityContent()
    {
        var act = () => ActivityResponseContract.Parse(ProgramContentType.Lesson, "{}", null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not accept*");
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"kind\":1}")]
    public void Parse_RequiresStringKindOnObject(string payload)
    {
        var act = () => ActivityResponseContract.Parse(ProgramContentType.Reflection, payload, null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*include a kind*");
    }

    [Fact]
    public void Serialize_UsesRuntimeResponseType()
    {
        ActivityResponse response = new ReflectionActivityResponse("A useful reflection");

        var payload = ActivityResponseContract.Serialize(response);
        using var json = JsonDocument.Parse(payload);

        json.RootElement.GetProperty("kind").GetString().Should().Be("reflection");
        json.RootElement.GetProperty("body").GetString().Should().Be("A useful reflection");
    }

    [Fact]
    public void ParseDiscussion_RejectsReplyWhenRepliesAreDisabled()
    {
        var rootId = Guid.NewGuid();
        var payload = $$"""{"kind":"discussion","body":"A reply","threadRootId":"{{rootId}}"}""";
        var settings = new DiscussionActivitySettings(AllowReplies: false);

        var act = () => ActivityResponseContract.Parse(ProgramContentType.Discussion, payload, settings);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*replies are disabled*");
    }

    [Fact]
    public void ParseSurvey_RejectsEmptyAnswers()
    {
        var act = () => ActivityResponseContract.Parse(
            ProgramContentType.Survey,
            "{\"kind\":\"survey\",\"answers\":{}}",
            null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one answer*");
    }

    [Theory]
    [InlineData("42")]
    [InlineData("\"not-a-guid\"")]
    public void ParseDiscussion_RejectsInvalidThreadRootId(string threadRootJson)
    {
        var payload = $$"""{"kind":"discussion","body":"A reply","threadRootId":{{threadRootJson}}}""";

        var act = () => ActivityResponseContract.Parse(ProgramContentType.Discussion, payload, null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*threadRootId must be a GUID*");
    }

    [Fact]
    public void CreateDefaultSettings_RejectsNonActivityContent()
    {
        var act = () => LearningActivityContract.CreateDefaultSettings(ProgramContentType.Lesson);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not support activity settings*");
    }

    [Fact]
    public void ContentInteractionCollection_ToDtoMapsEveryItem()
    {
        var first = new ContentInteraction { Id = Guid.NewGuid() };
        var second = new ContentInteraction { Id = Guid.NewGuid() };

        var result = new[] { first, second }.ToDto().ToArray();

        result.Select(item => item.Id).Should().Equal(first.Id, second.Id);
    }
}
