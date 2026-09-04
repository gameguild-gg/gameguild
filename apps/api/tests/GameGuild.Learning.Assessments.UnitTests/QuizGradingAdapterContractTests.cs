using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using GameGuild.Learning.Assessments;
using GameGuild.Learning.Assessments.Grading.Abstractions;
using GameGuild.Learning.Assessments.Grading.Capabilities;
using GameGuild.Learning.Assessments.Grading.Contracts;
using GameGuild.Learning.Assessments.QuizAdapter;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

public sealed class QuizGradingAdapterContractTests
{
    [Fact]
    public void Decoder_AcceptsTheSharedFixtureWithAllFourteenAnswerVariants()
    {
        var envelope = JsonSerializer.Deserialize<AssessmentResponseEnvelopeV1>(ReadFixture(), GradingJson.Options)!;
        var decoded = new QuizAnswerDecoder().Decode(envelope);

        decoded.GetProperty("answers").EnumerateObject().Should().HaveCount(14);
        decoded.GetProperty("answers").GetProperty("matching").GetProperty("matches")
            .GetProperty("left").GetString().Should().Be("right");
    }

    [Fact]
    public void Decoder_RejectsUnknownFieldsAndTextEncodedStructures()
    {
        var envelope = Envelope("""
            {"answers":{"matching":{"type":"MATCHING","matches":"left:right"}}}
            """);

        Action action = () => new QuizAnswerDecoder().Decode(envelope);

        action.Should().Throw<JsonException>();
    }

    [Fact]
    public void DeliveryGenerator_RemovesPrivateAnswerMaterial()
    {
        using var entry = JsonDocument.Parse("""
            {
              "type":"MATCHING",
              "stem":"Match",
              "points":"00000002.0000",
              "settings":{"allowRetry":false},
              "pairs":[{"id":"a","left":"A","right":"secret"}],
              "allowPartialCredit":true,
              "feedback":{"correct":"private","general":"visible"},
              "attachments":{"learnerVisible":[],"authorOnly":[{"assetUri":"asset:private","role":"answer"}]}
            }
            """);
        var projection = new QuizItemProjector().Project("q1", entry.RootElement);

        var delivery = new QuizDeliveryGenerator().Generate(projection);
        var serialized = delivery.GetRawText();

        var learnerEntry = delivery.GetProperty("entry");
        learnerEntry.GetProperty("pairs")[0].TryGetProperty("right", out _).Should().BeFalse();
        learnerEntry.GetProperty("attachments").TryGetProperty("authorOnly", out _).Should().BeFalse();
        learnerEntry.GetProperty("feedback").TryGetProperty("correct", out _).Should().BeFalse();
        serialized.Should().Contain("visible");
    }

    [Fact]
    public async Task Algorithm_PreservesPartialResultsAndExactPartialCredit()
    {
        using var trueFalse = JsonDocument.Parse("""
            {"type":"TRUE_FALSE","stem":"True","points":"00000002.0000","correctAnswer":true,"settings":{"allowRetry":false}}
            """);
        using var matching = JsonDocument.Parse("""
            {"type":"MATCHING","stem":"Match","points":"00000003.0000","pairs":[{"id":"a","left":"A","right":"1"},{"id":"b","left":"B","right":"2"},{"id":"c","left":"C","right":"3"}],"allowPartialCredit":true,"settings":{"allowRetry":false}}
            """);
        using var essay = JsonDocument.Parse("""
            {"type":"ESSAY","stem":"Explain","points":"00000004.0000","settings":{"allowRetry":false}}
            """);
        using var answers = JsonDocument.Parse("""
            {"answers":{"true-false":{"type":"TRUE_FALSE","value":true},"matching":{"type":"MATCHING","matches":{"a":"1","b":"wrong","c":"3"}},"essay":{"type":"ESSAY","richText":null,"plainText":"Response"}}}
            """);
        var projector = new QuizItemProjector();
        var projections = new[]
        {
            projector.Project("true-false", trueFalse.RootElement),
            projector.Project("matching", matching.RootElement),
            projector.Project("essay", essay.RootElement),
        };
        var request = new DeterministicReviewRequest(projections, EmptyDelivery(), answers.RootElement.Clone());

        var result = await new QuizDeterministicReviewAlgorithm().EvaluateAsync(request, CancellationToken.None);

        result.State.Should().Be("partial");
        result.Score.Should().BeNull();
        result.MaxScore.Should().Be(ScoreValue.Parse("00000009.0000"));
        result.Items[0].Score.Should().Be(ScoreValue.Parse("00000002.0000"));
        result.Items[1].Score.Should().Be(ScoreValue.Parse("00000002.0000"));
        result.Items[2].State.Should().Be(GradeItemState.Pending);
    }

    [Fact]
    public void Capability_IsAvailableForAuthorTestButNotOfficialSubmission()
    {
        var registry = new ReviewCapabilityRegistry();
        new QuizCapabilityRegistration().Register(registry);

        registry.ResolveReview(ReviewMethod.AutomatedReview, "quiz-automated-review", "1", ReviewExecutionContext.AuthorTest)
            .Should().NotBeNull();
        registry.ResolveReview(ReviewMethod.AutomatedReview, "quiz-automated-review", "1", ReviewExecutionContext.OfficialSubmission)
            .Should().BeNull();
    }

    [Fact]
    public void CoreAssembly_DoesNotReferenceTheQuizAdapter()
    {
        typeof(ReviewCapabilityRegistry).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Should().NotContain("GameGuild.Learning.Assessments.QuizAdapter");
        typeof(QuizItemProjector).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Should().Contain(typeof(ReviewCapabilityRegistry).Assembly.GetName().Name);
    }

    [Fact]
    public void Projector_RejectsAnIncompleteAuthoringEntryBeforeSnapshotPreparation()
    {
        using var entry = JsonDocument.Parse("""
            {"type":"TRUE_FALSE","stem":"Incomplete","settings":{"allowRetry":false}}
            """);

        var project = () => new QuizItemProjector().Project("q1", entry.RootElement);

        project.Should().Throw<JsonException>();
    }

    [Fact]
    public void ComponentResolver_UsesExactManifestKeysVersionsAndContext()
    {
        var services = new ServiceCollection();
        services.AddAssessmentsModule();
        services.AddQuizGradingAdapter();
        using var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<IAssessmentExecutionComponentResolver>();

        resolver.ResolveAnswerDecoder("quiz", "quiz-answer-decoder", "1", ReviewExecutionContext.AuthorTest)
            .Should().BeOfType<QuizAnswerDecoder>();
        provider.GetRequiredService<IReviewStageHandlerResolver>()
            .Resolve(ReviewMethod.AutomatedReview, "quiz-automated-review", "1", ReviewExecutionContext.AuthorTest)
            .Should().BeOfType<QuizAutomatedReviewHandler>();
        var wrongVersion = () => resolver.ResolveAnswerDecoder(
            "quiz", "quiz-answer-decoder", "2", ReviewExecutionContext.AuthorTest);
        var wrongContext = () => resolver.ResolveAnswerDecoder(
            "quiz", "quiz-answer-decoder", "1", ReviewExecutionContext.OfficialSubmission);
        wrongVersion.Should().Throw<InvalidOperationException>().WithMessage("*unavailable*");
        wrongContext.Should().Throw<InvalidOperationException>().WithMessage("*unavailable*");
    }

    [Fact]
    public async Task AutomatedHandler_UsesTheFixedManifestAndRejectsAnswersOutsideIt()
    {
        var services = new ServiceCollection();
        services.AddAssessmentsModule();
        services.AddQuizGradingAdapter();
        using var provider = services.BuildServiceProvider();
        var handler = provider.GetRequiredService<IReviewStageHandlerResolver>().Resolve(
            ReviewMethod.AutomatedReview,
            QuizAdapterContracts.AutomatedReviewHandlerKey,
            QuizAdapterContracts.Version,
            ReviewExecutionContext.AuthorTest);

        using var authoringEntry = JsonDocument.Parse("""
            {"type":"TRUE_FALSE","stem":"True","points":"00000001.0000","correctAnswer":true,"settings":{"allowRetry":false}}
            """);
        using var authoringContent = JsonDocument.Parse("{}");
        var projection = new QuizItemProjector().Project("q1", authoringEntry.RootElement);
        var snapshot = new AssessmentExecutionSnapshotV1(
            1,
            new AssessmentAuthoringSourceV1(
                1,
                "quiz",
                authoringContent.RootElement.Clone(),
                new ContentGradingDefinitionV2(
                    2,
                    new Dictionary<string, GradingItemAuthoringV2> { ["q1"] = new() }),
                AutomatedPolicy()),
            new AssessmentExecutionManifestV1(
                1,
                [new AssessmentItemManifestV1(
                    "q1",
                    "TRUE_FALSE",
                    QuizAdapterContracts.ProjectorKey,
                    QuizAdapterContracts.Version,
                    QuizAdapterContracts.DeliveryGeneratorKey,
                    QuizAdapterContracts.Version,
                    QuizAdapterContracts.AnswerDecoderKey,
                    QuizAdapterContracts.Version)],
                [new AssessmentReviewStageManifestV1(
                    ReviewMethod.AutomatedReview,
                    QuizAdapterContracts.AutomatedReviewHandlerKey,
                    QuizAdapterContracts.Version,
                    QuizAdapterContracts.DeterministicAlgorithmKey,
                    QuizAdapterContracts.Version)],
                []),
            new Dictionary<string, JsonElement> { ["q1"] = projection });
        const string snapshotHash = "0000000000000000000000000000000000000000000000000000000000000000";
        var delivery = new AssessmentExecutionDeliveryV1(
            1,
            Guid.NewGuid(),
            snapshotHash,
            ["q1"],
            new Dictionary<string, AssessmentExecutionDeliveryItemV1>
            {
                ["q1"] = new(
                    QuizAdapterContracts.DeliveryGeneratorKey,
                    QuizAdapterContracts.Version,
                    new QuizDeliveryGenerator().Generate(projection)),
            });
        var request = new ReviewStageRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ReviewExecutionContext.AuthorTest,
            snapshotHash,
            snapshot,
            delivery,
            Envelope("""{"answers":{"q1":{"type":"TRUE_FALSE","value":true}}}"""),
            null);

        var result = await handler.ExecuteAsync(request, CancellationToken.None);

        result.State.Should().Be("final");
        result.Score.Should().Be(ScoreValue.Parse("00000001.0000"));

        var unknownAnswer = request with
        {
            Response = Envelope("""{"answers":{"q2":{"type":"TRUE_FALSE","value":true}}}"""),
        };
        var invalid = async () => await handler.ExecuteAsync(unknownAnswer, CancellationToken.None);
        await invalid.Should().ThrowAsync<JsonException>().WithMessage("*unknown item q2*");
    }

    private static AssessmentResponseEnvelopeV1 Envelope(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        return new AssessmentResponseEnvelopeV1(1, "quiz", "quiz-answer/v1", document.RootElement.Clone());
    }

    private static AssessmentExecutionDeliveryV1 EmptyDelivery() => new(
        1,
        Guid.NewGuid(),
        "snapshot",
        [],
        new Dictionary<string, AssessmentExecutionDeliveryItemV1>());

    private static AssessmentExecutionPolicyV1 AutomatedPolicy() => new(
        1,
        null,
        1,
        null,
        null,
        new AssessmentAvailabilityPolicyV1(null, null, null, false, null),
        new AssessmentContentCompletionPolicyV1(ContentCompletionMode.OnRelease),
        new AssessmentResultReleasePolicyV1(ResultReleaseMode.Manual),
        new AssessmentPresentationPolicyV1("continuous"),
        new AssessmentReviewPolicyV1(1, ReviewMethods.AutomatedReview));

    private static string ReadFixture()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("GameGuild.Grading.Fixtures.quiz-answer-envelope-v1.json")
            ?? throw new InvalidOperationException("Shared quiz answer fixture was not embedded.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
