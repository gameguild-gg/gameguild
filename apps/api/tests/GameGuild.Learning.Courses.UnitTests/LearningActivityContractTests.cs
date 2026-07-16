using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests;

public sealed class LearningActivityContractTests
{
    [Fact]
    public void DiscussionSettings_ShouldRoundTripThroughContentAndDto()
    {
        var settings = new DiscussionActivitySettings(
            AllowReplies: true,
            RequireThreadRoot: true,
            MinimumBodyLength: 20,
            MaximumBodyLength: 500);
        var content = new ProgramContent { Type = ProgramContentType.Discussion };

        content.SetActivitySettings(settings);

        content.GetActivitySettings().Should().Be(settings);
        content.ToDto().ActivitySettings.Should().Be(settings);
    }

    [Theory]
    [InlineData(ProgramContentType.Discussion, "reflection")]
    [InlineData(ProgramContentType.Reflection, "survey")]
    [InlineData(ProgramContentType.Survey, "discussion")]
    public void ParseResponse_WhenPayloadKindDoesNotMatchContentType_ShouldReject(
        ProgramContentType contentType,
        string payloadKind)
    {
        var payload = $$"""{"kind":"{{payloadKind}}","body":"valid response"}""";

        var action = () => ActivityResponseContract.Parse(contentType, payload, null);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Activity response kind does not match*");
    }

    [Theory]
    [InlineData(ProgramContentType.Discussion, "{")]
    [InlineData(ProgramContentType.Reflection, "{\"kind\":\"reflection\"}")]
    [InlineData(ProgramContentType.Survey, "{\"kind\":\"survey\",\"answers\":[]}")]
    public void ParseResponse_WhenPayloadIsMalformedOrIncomplete_ShouldReject(
        ProgramContentType contentType,
        string payload)
    {
        var action = () => ActivityResponseContract.Parse(contentType, payload, null);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ParseResponse_ShouldValidateDiscussionAndReflectionBodyPolicies()
    {
        var discussionSettings = new DiscussionActivitySettings(true, false, 5, 20);
        var reflectionSettings = new ReflectionActivitySettings(true, 10, 20);

        var validDiscussion = ActivityResponseContract.Parse(
            ProgramContentType.Discussion,
            """{"kind":"discussion","body":"hello"}""",
            discussionSettings);
        var validReflection = ActivityResponseContract.Parse(
            ProgramContentType.Reflection,
            """{"kind":"reflection","body":"private note"}""",
            reflectionSettings);
        var invalidDiscussion = () => ActivityResponseContract.Parse(
            ProgramContentType.Discussion,
            """{"kind":"discussion","body":"no"}""",
            discussionSettings);
        var invalidReflection = () => ActivityResponseContract.Parse(
            ProgramContentType.Reflection,
            """{"kind":"reflection","body":"short"}""",
            reflectionSettings);

        validDiscussion.Should().BeOfType<DiscussionActivityResponse>();
        validReflection.Should().BeOfType<ReflectionActivityResponse>();
        invalidDiscussion.Should().Throw<InvalidOperationException>();
        invalidReflection.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Survey_ShouldNeverAcceptGrading()
    {
        var survey = new ProgramContent { Type = ProgramContentType.Survey };

        var action = () => survey.SetGrading(GradingMethod.Instructor, 100);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Surveys cannot be graded*");
    }

    [Fact]
    public void NormalizeLearningContract_WhenSurveyContainsLegacyGrading_ShouldClearIt()
    {
        var survey = new ProgramContent
        {
            Type = ProgramContentType.Survey,
            GradingMethod = GradingMethod.Instructor,
            MaxPoints = 100,
        };

        survey.NormalizeLearningContract();

        survey.GradingMethod.Should().Be(GradingMethod.None);
        survey.MaxPoints.Should().BeNull();
    }

    [Fact]
    public void ToDto_WhenLegacySurveyContainsGrading_ShouldNotExposeIt()
    {
        var survey = new ProgramContent
        {
            Type = ProgramContentType.Survey,
            GradingMethod = GradingMethod.Instructor,
            MaxPoints = 100,
        };

        var dto = survey.ToDto();

        dto.GradingMethod.Should().Be(GradingMethod.None);
        dto.MaxPoints.Should().BeNull();
    }

    [Fact]
    public void AnonymousSurveyResult_ShouldRetainAuditIdentityWithoutExposingIt()
    {
        var interaction = new ContentInteraction
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ProgramUserId = Guid.NewGuid(),
            SubmissionData = """{"kind":"survey","answers":{"experience":5}}""",
            SubmittedAt = SystemClock.UtcNow,
        };

        var result = SurveyResponseResultDto.FromInteraction(interaction);
        var serialized = JsonSerializer.Serialize(result);

        interaction.UserId.Should().NotBeEmpty();
        interaction.ProgramUserId.Should().NotBeEmpty();
        serialized.Should().NotContain(interaction.UserId.ToString());
        serialized.Should().NotContain(interaction.ProgramUserId.ToString());
        result.ResponseId.Should().Be(interaction.Id);
        result.Answers["experience"].GetInt32().Should().Be(5);
    }

    [Fact]
    public void LegacySubmissionData_ShouldRemainAvailableThroughTheExistingInteractionDto()
    {
        var interaction = new ContentInteraction { SubmissionData = "legacy free-form submission" };

        interaction.ToDto().SubmissionData.Should().Be("legacy free-form submission");
    }
}
