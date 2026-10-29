using System.Text.Json;
using FluentAssertions;
using GameGuild;
using GameGuild.AI;
using GameGuild.API.Core.Integration;
using GameGuild.Learning.Assessments.Grading.Abstractions;
using GameGuild.Learning.Assessments.Grading.Contracts;
using GameGuild.Learning.Grading.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GameGuild.API.UnitTests.Core.Integration;

public sealed class GradingAIReviewProviderTests
{
    [Theory]
    [InlineData(false, true, null, "grading-ai-disabled")]
    [InlineData(true, false, "AI.Disabled", "AI.Disabled")]
    [InlineData(true, false, "AI.CredentialsMissing", "AI.CredentialsMissing")]
    [InlineData(true, false, "AI.ModelNotConfigured", "AI.ModelNotConfigured")]
    public void ReadinessFailsClosedWithoutExposingConfiguration(
        bool gradingEnabled,
        bool backendAvailable,
        string? backendCode,
        string expectedCode)
    {
        var provider = CreateProvider(
            new RecordingAiOrchestrator("{}"),
            gradingEnabled,
            new StubAiBackendCatalog(new AiBackendReadinessDto(
                backendAvailable,
                backendCode,
                backendAvailable ? null : "Sanitized backend diagnostic.")));

        var readiness = provider.GetReadiness();

        readiness.Available.Should().BeFalse();
        readiness.Code.Should().Be(expectedCode);
        readiness.Message.Should().NotContain("secret", because: "readiness diagnostics must never expose credentials");
    }

    [Fact]
    public async Task ReviewUsesServerDerivedActorAndVersionedGenerationPolicy()
    {
        const string completion =
            """
            {"schemaVersion":1,"items":[{"itemId":"q1","state":"graded","score":75,"feedback":"Good reasoning."}],"feedback":"Reviewed."}
            """;
        var orchestrator = new RecordingAiOrchestrator(completion);
        var provider = CreateProvider(orchestrator);
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var invocation = CreateInvocation(tenantId, actorId);

        var response = await provider.ReviewAsync(invocation, CancellationToken.None);

        orchestrator.Actor.Should().Be(new AiExecutionActor(tenantId, actorId));
        orchestrator.Request.Should().NotBeNull();
        orchestrator.Request!.Backend.Should().Be("primary");
        orchestrator.Request.Model.Should().Be("grading-model");
        orchestrator.Request.Temperature.Should().Be(0);
        orchestrator.Request.MaxTokens.Should().Be(2048);
        orchestrator.Request.SystemPrompt.Should().Contain("Return exactly one JSON object");
        orchestrator.Request.Prompt.Should().Contain("<assessment-data>");
        orchestrator.Request.Prompt.Should().Contain(invocation.Request.RequestId.ToString());
        response.ModelKey.Should().Be("StandardAi");
        response.ModelVersion.Should().Be("resolved-model");
        response.Result.State.Should().Be("final");
        response.Result.Score.Should().Be(ScoreValue.FromUnits(75));
        response.Result.Items.Should().ContainSingle().Which.Feedback.Should().Be("Good reasoning.");
    }

    [Theory]
    [InlineData("{not-json")]
    [InlineData("{\"schemaVersion\":1,\"items\":[{\"itemId\":\"q1\",\"state\":\"graded\",\"score\":101,\"feedback\":null}],\"feedback\":null}")]
    [InlineData("{\"schemaVersion\":1,\"items\":[{\"itemId\":\"unknown\",\"state\":\"graded\",\"score\":100,\"feedback\":null}],\"feedback\":null}")]
    [InlineData("{\"schemaVersion\":1,\"items\":[],\"feedback\":null,\"unexpected\":true}")]
    public async Task InvalidProviderOutputNeverProducesAGrade(string completion)
    {
        var provider = CreateProvider(new RecordingAiOrchestrator(completion));

        var review = () => provider.ReviewAsync(
            CreateInvocation(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None).AsTask();

        await review.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task UnresolvedItemRequiresInstructorHandoff()
    {
        const string completion =
            """
            {"schemaVersion":1,"items":[{"itemId":"q1","state":"unsupported","score":null,"feedback":"Requires human judgment."}],"feedback":null}
            """;
        var provider = CreateProvider(new RecordingAiOrchestrator(completion));

        var withoutInstructor = () => provider.ReviewAsync(
            CreateInvocation(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None).AsTask();
        var withInstructor = await provider.ReviewAsync(
            CreateInvocation(Guid.NewGuid(), Guid.NewGuid(), withInstructorReview: true),
            CancellationToken.None);

        await withoutInstructor.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*without an instructor handoff*");
        withInstructor.Result.State.Should().Be("partial");
        withInstructor.Result.Score.Should().BeNull();
        withInstructor.Result.Items.Should().ContainSingle().Which.State.Should().Be(GradeItemState.Unsupported);
    }

    [Fact]
    public async Task OrchestratorFailureReturnsOnlySanitizedErrorCode()
    {
        var provider = CreateProvider(new RecordingAiOrchestrator(
            "unused",
            Error.Problem("AI.ProviderUnavailable", "secret-provider-message")));

        var review = () => provider.ReviewAsync(
            CreateInvocation(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None).AsTask();

        var exception = await review.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Contain("AI.ProviderUnavailable");
        exception.Which.Message.Should().NotContain("secret-provider-message");
    }

    private static GradingAIReviewProvider CreateProvider(
        RecordingAiOrchestrator orchestrator,
        bool gradingEnabled = true,
        IAiBackendCatalog? backendCatalog = null)
    {
        var gradingOptions = new GradingAIReviewOptions
        {
            Enabled = gradingEnabled,
            ProviderKey = GradingAIReviewOptions.DefaultProviderKey,
            PolicyVersion = GradingAIReviewOptions.SupportedPolicyVersion,
            Backend = "primary",
            Model = "grading-model",
            Temperature = 0,
            MaxTokens = 2048,
        };
        return new GradingAIReviewProvider(
            orchestrator,
            backendCatalog ?? new StubAiBackendCatalog(new AiBackendReadinessDto(
                true,
                Backend: "primary",
                Adapter: "StandardAi",
                Model: "grading-model")),
            Options.Create(gradingOptions),
            NullLogger<GradingAIReviewProvider>.Instance);
    }

    private static AIReviewProviderInvocationV1 CreateInvocation(
        Guid tenantId,
        Guid actorId,
        bool withInstructorReview = false)
    {
        const string itemId = "q1";
        var methods = ReviewMethods.AIReview |
                      (withInstructorReview ? ReviewMethods.InstructorReview : ReviewMethods.None);
        var stages = new List<AssessmentReviewStageManifestV1>
        {
            new(
                ReviewMethod.AIReview,
                "ai-review",
                "1",
                GradingAIReviewOptions.DefaultProviderKey,
                GradingAIReviewOptions.SupportedPolicyVersion),
        };
        if (withInstructorReview)
        {
            stages.Add(new AssessmentReviewStageManifestV1(ReviewMethod.InstructorReview, "instructor-review", "1"));
        }

        var policy = new AssessmentExecutionPolicyV1(
            1,
            null,
            1,
            null,
            null,
            new AssessmentAvailabilityPolicyV1(null, null, null, false, null),
            new AssessmentContentCompletionPolicyV1(ContentCompletionMode.OnFinalize),
            new AssessmentResultReleasePolicyV1(ResultReleaseMode.Manual),
            new AssessmentPresentationPolicyV1("continuous"),
            new AssessmentReviewPolicyV1(
                1,
                methods,
                Ai: new AiReviewPolicyV1(
                    GradingAIReviewOptions.DefaultProviderKey,
                    GradingAIReviewOptions.SupportedPolicyVersion),
                Instructor: withInstructorReview ? new InstructorReviewPolicyV1(false) : null));
        var projection = JsonSerializer.SerializeToElement(new
        {
            schemaVersion = 1,
            itemId,
            itemType = "generic-item",
            maxScore = 100,
            source = new { contentType = "generic-assessment", itemId },
        });
        var snapshot = new AssessmentExecutionSnapshotV1(
            1,
            new AssessmentAuthoringSourceV1(
                1,
                "generic-assessment",
                JsonSerializer.SerializeToElement(new { title = "Untrusted assessment content" }),
                new ContentGradingDefinitionV2(
                    2,
                    new Dictionary<string, GradingItemAuthoringV2>(StringComparer.Ordinal)
                    {
                        [itemId] = new(),
                    }),
                policy),
            new AssessmentExecutionManifestV1(
                1,
                [new AssessmentItemManifestV1(itemId, "generic-item", "generic-adapter", "1")],
                stages,
                [new AssessmentPolicyManifestV1("grading.execution-policy", "1")]),
            new Dictionary<string, JsonElement>(StringComparer.Ordinal) { [itemId] = projection });
        var executionSnapshotHash = new string('a', 64);
        var delivery = new AssessmentExecutionDeliveryV1(
            1,
            Guid.NewGuid(),
            executionSnapshotHash,
            [itemId],
            new Dictionary<string, AssessmentExecutionDeliveryItemV1>(StringComparer.Ordinal)
            {
                [itemId] = new("generic-adapter", "1", JsonSerializer.SerializeToElement(new { prompt = "Data only" })),
            });
        var response = new AssessmentResponseEnvelopeV1(
            1,
            "generic-assessment",
            "generic-response/v1",
            JsonSerializer.SerializeToElement(new { answers = new { q1 = "Student response" } }));
        var requestId = Guid.NewGuid();
        var request = new AIReviewProviderRequestV1(
            1,
            requestId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            requestId,
            ReviewExecutionContext.OfficialSubmission,
            executionSnapshotHash,
            snapshot,
            delivery,
            response,
            null,
            GradingAIReviewOptions.DefaultProviderKey,
            GradingAIReviewOptions.SupportedPolicyVersion);
        return new AIReviewProviderInvocationV1(1, tenantId, actorId, request);
    }

    private sealed class RecordingAiOrchestrator(
        string completion,
        Error? failure = null) : IAiOrchestrator
    {
        public AiExecutionActor? Actor { get; private set; }
        public AiGenerateRequest? Request { get; private set; }

        public Task<Result<AiCompletionResponse>> GenerateForActorAsync(
            AiExecutionActor actor,
            AiGenerateRequest request,
            CancellationToken cancellationToken = default)
        {
            Actor = actor;
            Request = request;
            return Task.FromResult(failure is null
                ? Result.Success(new AiCompletionResponse(
                    "primary",
                    "StandardAi",
                    "resolved-model",
                    completion,
                    "stop",
                    new AiUsageDto(100, 20, 120)))
                : Result.Failure<AiCompletionResponse>(failure));
        }

        public Task<Result<AiCompletionResponse>> ChatAsync(
            AiChatRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<AiCompletionResponse>> GenerateAsync(
            AiGenerateRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<AiResolvedModelDto>> DescribeGenerateAsync(
            AiExecutionActor actor,
            AiGenerateRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<AiCompletionResponse>> GenerateForActorStreamingAsync(
            AiExecutionActor actor,
            AiGenerateRequest request,
            Func<string, CancellationToken, ValueTask> onDelta,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<AiCompletionResponse>> GenerateForActorStreamingWithReservedQuotaAsync(
            AiExecutionActor actor,
            AiGenerateRequest request,
            Func<string, CancellationToken, ValueTask> onDelta,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubAiBackendCatalog(AiBackendReadinessDto readiness) : IAiBackendCatalog
    {
        public AiBackendReadinessDto GetReadiness(string? backend = null, string? model = null) => readiness;

        public IReadOnlyList<AiBackendStatusDto> GetStatus() => [];
    }
}
