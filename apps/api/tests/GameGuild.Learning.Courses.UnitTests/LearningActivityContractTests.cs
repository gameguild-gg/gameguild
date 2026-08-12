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
    [MemberData(nameof(ActivitySettingsCases))]
    public void ActivitySettings_ShouldRoundTripForEveryActivityType(ProgramContentType type, ActivitySettings settings)
    {
        var content = new ProgramContent { Type = type };

        content.SetActivitySettings(settings);

        content.GetActivitySettings().Should().Be(settings);
        content.ToDto().ActivitySettings.Should().Be(settings);
    }

    public static IEnumerable<object[]> ActivitySettingsCases =>
    [
        [ProgramContentType.Discussion, new DiscussionActivitySettings(true, false, 2, 200)],
        [ProgramContentType.Discussion, new DiscussionActivitySettings(true, false, 1, 1)],
        [ProgramContentType.Reflection, new ReflectionActivitySettings(true, 3, 300)],
        [ProgramContentType.Reflection, new ReflectionActivitySettings(true, 1, 1)],
        [ProgramContentType.Survey, new SurveyActivitySettings(true, true, SurveyResultsVisibility.AfterClose)],
    ];

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
    public void ParseResponse_WhenDiscussionRequiresThreadRoot_ShouldRejectTopLevelResponse()
    {
        var settings = new DiscussionActivitySettings(AllowReplies: true, RequireThreadRoot: true);

        var missingRoot = () => ActivityResponseContract.Parse(
            ProgramContentType.Discussion,
            """{"kind":"discussion","body":"reply"}""",
            settings);
        var rooted = ActivityResponseContract.Parse(
            ProgramContentType.Discussion,
            $$"""{"kind":"discussion","body":"reply","threadRootId":"{{Guid.NewGuid()}}"}""",
            settings);

        missingRoot.Should().Throw<InvalidOperationException>()
            .WithMessage("Discussion responses require a thread root.*");
        rooted.Should().BeOfType<DiscussionActivityResponse>();
    }

    [Theory]
    [MemberData(nameof(InvalidSettingsCases))]
    public void ValidateSettings_WhenSettingsAreInvalidOrForAnotherActivity_ShouldReject(
        ProgramContentType contentType,
        ActivitySettings settings)
    {
        var action = () => LearningActivityContract.ValidateSettings(contentType, settings);

        action.Should().Throw<InvalidOperationException>();
    }

    public static IEnumerable<object[]> InvalidSettingsCases =>
    [
        [ProgramContentType.Discussion, new DiscussionActivitySettings(MinimumBodyLength: 0)],
        [ProgramContentType.Discussion, new DiscussionActivitySettings(MinimumBodyLength: 10, MaximumBodyLength: 9)],
        [ProgramContentType.Reflection, new ReflectionActivitySettings(MinimumBodyLength: 0)],
        [ProgramContentType.Reflection, new ReflectionActivitySettings(MinimumBodyLength: 10, MaximumBodyLength: 9)],
        [ProgramContentType.Discussion, new DiscussionActivitySettings(AllowReplies: false, RequireThreadRoot: true)],
        [ProgramContentType.Discussion, new ReflectionActivitySettings()],
        [ProgramContentType.Reflection, new SurveyActivitySettings()],
        [ProgramContentType.Survey, new SurveyActivitySettings(ResultsVisibility: (SurveyResultsVisibility)99)],
    ];

    [Theory]
    [MemberData(nameof(ActivitySettingsCases))]
    public void ValidateSettings_WhenSettingsAreAtValidBoundaries_ShouldAccept(
        ProgramContentType contentType,
        ActivitySettings settings)
    {
        var action = () => LearningActivityContract.ValidateSettings(contentType, settings);

        action.Should().NotThrow();
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

    [Theory]
    [InlineData(ProgramContentType.Survey, true)]
    [InlineData(ProgramContentType.Lesson, false)]
    [InlineData(ProgramContentType.Assignment, false)]
    [InlineData(ProgramContentType.Discussion, false)]
    [InlineData(ProgramContentType.Reflection, false)]
    public void SurveyPolicyLock_Discriminator_ShouldOnlyLockSurveys(ProgramContentType type, bool expected)
    {
        LearningActivityContract.RequiresSurveyPolicyLock(type).Should().Be(expected);
    }
}
