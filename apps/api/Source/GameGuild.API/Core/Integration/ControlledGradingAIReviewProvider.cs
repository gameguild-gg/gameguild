using System.Text.Json;
using GameGuild.Learning.Assessments.Grading.Abstractions;
using GameGuild.Learning.Assessments.Grading.Contracts;
using GameGuild.Learning.Grading.Contracts;

namespace GameGuild.API.Core.Integration;

/// <summary>
/// Deterministic provider used only by the local grading-validation stack.
/// Registration is guarded by both host environment and explicit E2E configuration.
/// </summary>
internal sealed class ControlledGradingAIReviewProvider : IAIReviewProvider, IAIReviewProviderReadiness
{
    public const string ProviderKey = "controlled-ai";
    public const string PolicyVersion = "v1";

    private static readonly IReadOnlySet<string> Policies =
        new HashSet<string>(StringComparer.Ordinal) { PolicyVersion };

    private static readonly IReadOnlySet<ReviewExecutionContext> SupportedContexts =
        new HashSet<ReviewExecutionContext>
        {
            ReviewExecutionContext.AuthorTest,
            ReviewExecutionContext.OfficialSubmission,
        };

    public string Key => ProviderKey;
    public IReadOnlySet<string> PolicyVersions => Policies;
    public IReadOnlySet<ReviewExecutionContext> Contexts => SupportedContexts;

    public AIReviewProviderReadinessV1 GetReadiness() => new(true);

    public ValueTask<AIReviewProviderResponseV1> ReviewAsync(
        AIReviewProviderInvocationV1 invocation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(invocation);
        var request = invocation.Request;
        if (invocation.SchemaVersion != 1 || invocation.TenantId == Guid.Empty || invocation.ActorId == Guid.Empty ||
            request.SchemaVersion != 1 || request.RequestId != request.ReviewStageId ||
            !string.Equals(request.ProviderKey, ProviderKey, StringComparison.Ordinal) ||
            !string.Equals(request.ProviderPolicyVersion, PolicyVersion, StringComparison.Ordinal) ||
            !SupportedContexts.Contains(request.Context))
        {
            throw new ArgumentException("Controlled AI review invocation is invalid.", nameof(invocation));
        }

        GradingContractValidator.ValidateBindings(
            request.ExecutionSnapshotHash,
            request.Snapshot,
            request.Delivery,
            request.Response);
        var evidenceRef = $"ai-request:{request.RequestId:N}";
        var items = request.Snapshot.Manifest.Items.Select(item =>
        {
            var maximum = ReadMaximum(request.Snapshot.ItemProjections[item.ItemId]);
            return new GradeItemResultV1(
                item.ItemId,
                GradeItemState.Graded,
                maximum,
                maximum,
                [evidenceRef],
                ReviewMethod.AIReview,
                "ai-review",
                "1",
                "Controlled grading-validation review.",
                ProviderKey);
        }).ToArray();
        var result = new GradeResultV1(
            GradingContractVersions.GradeResult,
            "final",
            ScoreValue.Sum(items.Select(item => item.Score!.Value)),
            ScoreValue.Sum(items.Select(item => item.MaxScore)),
            items,
            [evidenceRef],
            "Controlled grading-validation review.");
        GradingContractValidator.Validate(result);

        return ValueTask.FromResult(new AIReviewProviderResponseV1(
            1,
            request.RequestId,
            request.GradingExecutionId,
            request.GradeRoundId,
            request.ReviewStageId,
            request.ExecutionSnapshotHash,
            request.ProviderKey,
            request.ProviderPolicyVersion,
            "controlled-validation",
            "v1",
            result));
    }

    private static ScoreValue ReadMaximum(JsonElement projection) =>
        ScoreValue.FromUnits(projection.GetProperty("maxScore").GetInt32());
}
