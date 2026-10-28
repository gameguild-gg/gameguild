using System.Text.Json;
using GameGuild.Learning.Assessments.Grading.Abstractions;
using GameGuild.Learning.Assessments.Grading.Contracts;
using GameGuild.Learning.Assessments.Grading.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Learning.Assessments.Grading.Runtime;

public sealed class AIReviewStageHandler : IReviewStageHandler
{
    private static readonly IReadOnlySet<ReviewExecutionContext> SupportedContexts =
        new HashSet<ReviewExecutionContext>
        {
            ReviewExecutionContext.AuthorTest,
            ReviewExecutionContext.OfficialSubmission,
        };

    public ReviewMethod Method => ReviewMethod.AIReview;
    public string Key => "ai-review";
    public string Version => "1";
    public string? ProviderKey => null;
    public string? ProviderPolicyVersion => null;
    public IReadOnlySet<ReviewExecutionContext> Contexts => SupportedContexts;

    public ValueTask<ReviewStageTransitionV1> StartAsync(
        ReviewStageRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var providerKey = RequireProvider(request.ProviderKey, "provider key");
        var providerPolicyVersion = RequireProvider(request.ProviderPolicyVersion, "provider policy version");
        var providerRequest = new AIReviewProviderRequestV1(
            1,
            request.ReviewStageId,
            request.GradingExecutionId,
            request.GradeRoundId,
            request.ReviewStageId,
            request.Context,
            request.ExecutionSnapshotHash,
            request.Snapshot,
            request.Delivery,
            request.Response,
            request.PreviousStageResult,
            providerKey,
            providerPolicyVersion);
        return ValueTask.FromResult(ReviewStageTransitionV1.AwaitingEvidence(
            "AI review request is pending.",
            durableRequest: new DurableReviewRequestV1(
                "ai-review-requested",
                "1",
                JsonSerializer.SerializeToElement(providerRequest, GradingJson.Options))));
    }

    public ValueTask<ReviewStageTransitionV1> AcceptEvidenceAsync(
        ReviewStageRequest request,
        ReviewStageEvidenceV1 evidence,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(evidence.EvidenceType, "ai-review-response", StringComparison.Ordinal) ||
            !string.Equals(evidence.SchemaVersion, "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("AIReview accepts only version 1 AI provider responses.");
        }

        return ValueTask.FromResult(Complete(request, evidence));
    }

    public ValueTask<ReviewStageTransitionV1> TryCompleteAsync(
        ReviewStageRequest request,
        IReadOnlyList<ReviewStageEvidenceV1> evidence,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var responses = evidence
            .Where(value => string.Equals(value.EvidenceType, "ai-review-response", StringComparison.Ordinal))
            .ToArray();
        return responses.Length switch
        {
            0 => ValueTask.FromResult(ReviewStageTransitionV1.AwaitingEvidence("AI review response is pending.")),
            1 => ValueTask.FromResult(Complete(request, responses[0])),
            _ => throw new InvalidOperationException("An AIReview stage cannot contain multiple provider responses."),
        };
    }

    private static ReviewStageTransitionV1 Complete(
        ReviewStageRequest request,
        ReviewStageEvidenceV1 evidence)
    {
        var response = evidence.Payload.Deserialize<AIReviewProviderResponseV1>(GradingJson.Options)
            ?? throw new JsonException("AI review response is invalid.");
        if (response.SchemaVersion != 1 ||
            response.RequestId != request.ReviewStageId ||
            response.GradingExecutionId != request.GradingExecutionId ||
            response.GradeRoundId != request.GradeRoundId ||
            response.ReviewStageId != request.ReviewStageId ||
            !string.Equals(response.ExecutionSnapshotHash, request.ExecutionSnapshotHash, StringComparison.Ordinal) ||
            !string.Equals(response.ProviderKey, request.ProviderKey, StringComparison.Ordinal) ||
            !string.Equals(response.ProviderPolicyVersion, request.ProviderPolicyVersion, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(response.ModelKey) ||
            string.IsNullOrWhiteSpace(response.ModelVersion))
        {
            throw new InvalidOperationException("AI review response does not match the frozen stage request.");
        }

        GradingContractValidator.Validate(response.Result);
        return ReviewStageTransitionV1.Completed(response.Result);
    }

    private static string RequireProvider(string? value, string label) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"AIReview {label} is required.")
            : value;
}

public sealed class AIReviewRequestConsumer(
    IAIReviewProviderRegistry providers,
    IApplicationDbContext context,
    IServiceProvider services) : IAcademicOutboxConsumer
{
    private const string ReceiptCommandType = "ai-review-provider-response";
    private const string ReceiptSchemaVersion = "ai-review-provider-response.v1";

    public string Key => "grading.ai-review-request.v1";

    public IReadOnlySet<string> EventTypes { get; } =
        new HashSet<string>(StringComparer.Ordinal) { "ai-review-requested" };

    public async Task ConsumeAsync(AcademicOutboxEvent message, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<AIReviewProviderRequestV1>(
            message.PayloadCanonicalJson,
            GradingJson.Options) ?? throw new JsonException("AI review request is invalid.");
        if (request.SchemaVersion != 1 || request.RequestId != request.ReviewStageId)
        {
            throw new JsonException("AI review request version or correlation is invalid.");
        }

        var actorId = await ResolveActorAsync(message, request, cancellationToken).ConfigureAwait(false);
        var response = await FindResponseAsync(message, request, actorId, cancellationToken).ConfigureAwait(false);
        if (response is null)
        {
            var provider = providers.Resolve(request.ProviderKey, request.ProviderPolicyVersion, request.Context);
            response = await provider.ReviewAsync(
                    new AIReviewProviderInvocationV1(1, message.TenantId, actorId, request),
                    cancellationToken)
                .ConfigureAwait(false);
            ValidateProviderResponse(request, response);
            await StoreResponseAsync(message, request, actorId, response, cancellationToken).ConfigureAwait(false);
        }

        var orchestrator = services.GetRequiredService<IGradingExecutionOrchestrator>();
        await orchestrator.AcceptReviewEvidenceAsync(
                request.GradingExecutionId,
                ReviewMethod.AIReview,
                new ReviewStageEvidenceV1(
                    $"ai-response:{request.RequestId:N}",
                    "ai-review-response",
                    "1",
                    JsonSerializer.SerializeToElement(response, GradingJson.Options),
                    ProducedByService: $"{response.ProviderKey}/{response.ModelKey}@{response.ModelVersion}"),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<Guid> ResolveActorAsync(
        AcademicOutboxEvent message,
        AIReviewProviderRequestV1 request,
        CancellationToken cancellationToken)
    {
        var execution = await context.Set<GradingExecution>()
            .AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == request.GradingExecutionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("AI review execution was not found.");
        if (execution.TenantId != message.TenantId || execution.ExecutionContext != request.Context)
        {
            throw new InvalidOperationException("AI review execution context does not match the durable request.");
        }

        Guid actorId;
        if (request.Context == ReviewExecutionContext.AuthorTest)
        {
            var subjectId = execution.TestRunSubjectId
                ?? throw new InvalidOperationException("AI author test execution is missing its subject.");
            actorId = await context.Set<AssessmentTestRunSubject>()
                .AsNoTracking()
                .Where(subject => subject.Id == subjectId && subject.TenantId == message.TenantId)
                .Join(
                    context.Set<AssessmentTestRun>().AsNoTracking(),
                    subject => subject.TestRunId,
                    run => run.Id,
                    (_, run) => run.CreatedByUserId)
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            var submissionId = execution.AssessmentSubmissionId
                ?? throw new InvalidOperationException("Official AI review execution is missing its submission.");
            actorId = await context.Set<AssessmentSubmission>()
                .AsNoTracking()
                .Where(submission => submission.Id == submissionId && submission.TenantId == message.TenantId)
                .Select(submission => submission.SubmittedByUserId ?? submission.StartedByUserId)
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        return actorId == Guid.Empty
            ? throw new InvalidOperationException("AI review could not resolve a trusted execution actor.")
            : actorId;
    }

    private async Task<AIReviewProviderResponseV1?> FindResponseAsync(
        AcademicOutboxEvent message,
        AIReviewProviderRequestV1 request,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        var receipt = await context.Set<GradingCommandReceipt>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                value => value.TenantId == message.TenantId &&
                         value.ResourceId == request.ReviewStageId &&
                         value.CommandType == ReceiptCommandType &&
                         value.ActorId == actorId &&
                         value.IdempotencyKey == request.RequestId.ToString("N"),
                cancellationToken)
            .ConfigureAwait(false);
        if (receipt is null)
        {
            return null;
        }

        if (!string.Equals(receipt.RequestHash, message.PayloadHash, StringComparison.Ordinal) ||
            !string.Equals(receipt.OutcomeSchemaVersion, ReceiptSchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("AI review idempotency receipt does not match the durable request.");
        }

        var response = JsonSerializer.Deserialize<AIReviewProviderResponseV1>(
            receipt.OutcomeCanonicalJson,
            GradingJson.Options) ?? throw new JsonException("Cached AI review response is invalid.");
        ValidateProviderResponse(request, response);
        return response;
    }

    private async Task StoreResponseAsync(
        AcademicOutboxEvent message,
        AIReviewProviderRequestV1 request,
        Guid actorId,
        AIReviewProviderResponseV1 response,
        CancellationToken cancellationToken)
    {
        var canonical = CanonicalJson.Serialize(JsonSerializer.SerializeToElement(response, GradingJson.Options));
        context.Set<GradingCommandReceipt>().Add(GradingCommandReceipt.Create(
            message.TenantId,
            request.ReviewStageId,
            ReceiptCommandType,
            actorId,
            request.RequestId.ToString("N"),
            message.PayloadHash,
            ReceiptSchemaVersion,
            canonical,
            SystemClock.UtcNow.AddDays(90)));
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateProviderResponse(
        AIReviewProviderRequestV1 request,
        AIReviewProviderResponseV1 response)
    {
        if (response.SchemaVersion != 1 ||
            response.RequestId != request.RequestId ||
            response.GradingExecutionId != request.GradingExecutionId ||
            response.GradeRoundId != request.GradeRoundId ||
            response.ReviewStageId != request.ReviewStageId ||
            !string.Equals(response.ExecutionSnapshotHash, request.ExecutionSnapshotHash, StringComparison.Ordinal) ||
            !string.Equals(response.ProviderKey, request.ProviderKey, StringComparison.Ordinal) ||
            !string.Equals(response.ProviderPolicyVersion, request.ProviderPolicyVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("AI review provider returned a response for a different request.");
        }

        GradingContractValidator.Validate(response.Result);
    }
}
