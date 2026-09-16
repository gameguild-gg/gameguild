using System.Text.Json;
using FluentAssertions;
using GameGuild.Identity.Users;
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetSettings_UsesDefaultsWhenSerializedSettingsAreBlank(string? serializedSettings)
    {
        var settings = LearningActivityContract.GetSettings(
            ProgramContentType.Discussion,
            serializedSettings);

        settings.Should().Be(new DiscussionActivitySettings());
    }

    [Fact]
    public void GetSettings_RejectsSerializedNullSettings()
    {
        var act = () => LearningActivityContract.GetSettings(
            ProgramContentType.Discussion,
            "null");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*settings are invalid*");
    }

    [Fact]
    public void SurveyFlags_RequireSurveyContentAndEnabledSettings()
    {
        var lesson = new ProgramContent
        {
            Type = ProgramContentType.Lesson,
            Title = "Lesson",
        };
        var disabledSurvey = new ProgramContent
        {
            Type = ProgramContentType.Survey,
            Title = "Survey",
        };
        var enabledSurvey = new ProgramContent
        {
            Type = ProgramContentType.Survey,
            Title = "Anonymous repeatable survey",
        };
        disabledSurvey.SetActivitySettings(new SurveyActivitySettings());
        enabledSurvey.SetActivitySettings(
            new SurveyActivitySettings(IsAnonymous: true, AllowMultipleResponses: true));

        LearningActivityContract.AllowsMultipleResponses(lesson).Should().BeFalse();
        LearningActivityContract.IsAnonymousSurvey(lesson).Should().BeFalse();
        LearningActivityContract.AllowsMultipleResponses(disabledSurvey).Should().BeFalse();
        LearningActivityContract.IsAnonymousSurvey(disabledSurvey).Should().BeFalse();
        LearningActivityContract.AllowsMultipleResponses(enabledSurvey).Should().BeTrue();
        LearningActivityContract.IsAnonymousSurvey(enabledSurvey).Should().BeTrue();
    }

    [Theory]
    [InlineData("{\"kind\":\"survey\"}")]
    [InlineData("{\"kind\":\"survey\",\"answers\":[]}")]
    public void ParseSurvey_RequiresAnswersObject(string payload)
    {
        var act = () => ActivityResponseContract.Parse(ProgramContentType.Survey, payload, null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*answers object*");
    }

    [Fact]
    public void ContentInteractionCollection_ToDtoMapsEveryItem()
    {
        var first = new ContentInteraction { Id = Guid.NewGuid() };
        var second = new ContentInteraction { Id = Guid.NewGuid() };

        var result = new[] { first, second }.ToDto().ToArray();

        result.Select(item => item.Id).Should().Equal(first.Id, second.Id);
    }

    [Fact]
    public void ContentInteraction_ToDtoMapsLoadedContentAndEnrollmentUser()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Learner",
            Email = "learner@example.com",
        };
        var interaction = new ContentInteraction
        {
            Content = new ProgramContent
            {
                Id = Guid.NewGuid(),
                Title = "Discussion",
                Type = ProgramContentType.Discussion,
                EstimatedMinutes = 8,
            },
            ProgramUser = new ProgramUser { User = user },
        };

        var dto = interaction.ToDto();

        dto.Content.Should().NotBeNull();
        dto.Content!.Title.Should().Be("Discussion");
        dto.ProgramUser.Should().NotBeNull();
        dto.ProgramUser!.Id.Should().Be(user.Id);
        dto.ProgramUser.UserDisplayName.Should().Be("Learner");
        dto.ProgramUser.UserEmail.Should().Be("learner@example.com");
    }

    [Fact]
    public void ContentInteraction_ToDtoOmitsEnrollmentSummaryWhenUserIsNotLoaded()
    {
        var interaction = new ContentInteraction
        {
            ProgramUser = new ProgramUser { User = null! },
        };

        interaction.ToDto().ProgramUser.Should().BeNull();
    }

    [Fact]
    public void ReflectionProjection_CanIncludeRespondentIdentity()
    {
        var userId = Guid.NewGuid();
        var interaction = new ContentInteraction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubmissionData = "{\"kind\":\"reflection\",\"body\":\"A reflection\"}",
        };

        var result = ReflectionResponseResultDto.FromInteraction(interaction, includeRespondentIdentity: true);

        result.RespondentUserId.Should().Be(userId);
    }

    [Fact]
    public void ReflectionProjection_RejectsMissingSubmissionData()
    {
        var act = () => ReflectionResponseResultDto.FromInteraction(new ContentInteraction());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*submission data is missing*");
    }

    [Fact]
    public void SurveyProjection_CanIncludeRespondentIdentity()
    {
        var userId = Guid.NewGuid();
        var interaction = new ContentInteraction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubmissionData = "{\"kind\":\"survey\",\"answers\":{\"choice\":\"A\"}}",
        };

        var result = SurveyResponseResultDto.FromInteraction(interaction, includeRespondentIdentity: true);

        result.RespondentUserId.Should().Be(userId);
    }

    [Fact]
    public void SurveyProjection_RejectsMissingSubmissionData()
    {
        var act = () => SurveyResponseResultDto.FromInteraction(new ContentInteraction());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*submission data is missing*");
    }
}
