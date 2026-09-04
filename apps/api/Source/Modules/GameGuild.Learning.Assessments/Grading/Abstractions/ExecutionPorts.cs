using System.Text.Json;
using GameGuild.Learning.Assessments.Grading.Contracts;

namespace GameGuild.Learning.Assessments.Grading.Abstractions;

public interface IAssessmentItemProjector
{
    string Key { get; }
    string Version { get; }
    string ContentType { get; }
    JsonElement Project(string itemId, JsonElement authoringItem);
}

public interface IAssessmentDeliveryGenerator
{
    string Key { get; }
    string Version { get; }
    string ContentType { get; }
    JsonElement Generate(JsonElement projectedItem);
}

public interface IAssessmentAnswerDecoder
{
    string Key { get; }
    string Version { get; }
    string ContentType { get; }
    JsonElement Decode(AssessmentResponseEnvelopeV1 envelope);
}

public interface IDeterministicReviewAlgorithm
{
    string Key { get; }
    string Version { get; }
    ValueTask<GradeResultV1> EvaluateAsync(
        DeterministicReviewRequest request,
        CancellationToken cancellationToken);
}

public sealed record DeterministicReviewRequest(
    IReadOnlyList<JsonElement> ProjectedItems,
    AssessmentExecutionDeliveryV1 Delivery,
    JsonElement NormalizedResponse);

public interface IReviewStageHandler
{
    ReviewMethod Method { get; }
    string Key { get; }
    string Version { get; }
    IReadOnlySet<ReviewExecutionContext> Contexts { get; }
    ValueTask<GradeResultV1> ExecuteAsync(ReviewStageRequest request, CancellationToken cancellationToken);
}

public interface IAssessmentExecutionComponentResolver
{
    IAssessmentItemProjector ResolveItemProjector(
        string contentType,
        string key,
        string version,
        ReviewExecutionContext context);

    IAssessmentDeliveryGenerator ResolveDeliveryGenerator(
        string contentType,
        string key,
        string version,
        ReviewExecutionContext context);

    IAssessmentAnswerDecoder ResolveAnswerDecoder(
        string contentType,
        string key,
        string version,
        ReviewExecutionContext context);

    IDeterministicReviewAlgorithm ResolveDeterministicAlgorithm(
        string key,
        string version,
        ReviewExecutionContext context);
}

public interface IReviewStageHandlerResolver
{
    IReviewStageHandler Resolve(
        ReviewMethod method,
        string key,
        string version,
        ReviewExecutionContext context);
}

public sealed record ReviewStageRequest(
    Guid GradingExecutionId,
    Guid GradeRoundId,
    ReviewExecutionContext Context,
    string ExecutionSnapshotHash,
    AssessmentExecutionSnapshotV1 Snapshot,
    AssessmentExecutionDeliveryV1 Delivery,
    AssessmentResponseEnvelopeV1 Response,
    GradeResultV1? PreviousStageResult);
