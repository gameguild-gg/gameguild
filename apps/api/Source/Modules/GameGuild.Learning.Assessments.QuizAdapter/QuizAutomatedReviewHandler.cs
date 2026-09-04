using GameGuild.Learning.Assessments.Grading.Abstractions;
using GameGuild.Learning.Assessments.Grading.Contracts;

namespace GameGuild.Learning.Assessments.QuizAdapter;

public sealed class QuizAutomatedReviewHandler(
    IAssessmentExecutionComponentResolver components) : IReviewStageHandler
{
    private static readonly IReadOnlySet<ReviewExecutionContext> SupportedContexts =
        new HashSet<ReviewExecutionContext> { ReviewExecutionContext.AuthorTest };

    public ReviewMethod Method => ReviewMethod.AutomatedReview;
    public string Key => QuizAdapterContracts.AutomatedReviewHandlerKey;
    public string Version => QuizAdapterContracts.Version;
    public IReadOnlySet<ReviewExecutionContext> Contexts => SupportedContexts;

    public ValueTask<GradeResultV1> ExecuteAsync(ReviewStageRequest request, CancellationToken cancellationToken)
    {
        if (!SupportedContexts.Contains(request.Context))
        {
            throw new InvalidOperationException($"Quiz automated review is unavailable in {request.Context}.");
        }

        GradingContractValidator.ValidateBindings(
            request.ExecutionSnapshotHash,
            request.Snapshot,
            request.Delivery,
            request.Response);

        var stage = request.Snapshot.Manifest.Stages.SingleOrDefault(candidate =>
            candidate.Method == Method &&
            string.Equals(candidate.HandlerKey, Key, StringComparison.Ordinal) &&
            string.Equals(candidate.HandlerVersion, Version, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"The execution manifest does not select {Method} handler {Key}@{Version}.");
        if (stage.AlgorithmKey is null || stage.AlgorithmVersion is null)
        {
            throw new InvalidOperationException("The automated review stage does not fix an algorithm version.");
        }

        var decoderIdentity = request.Snapshot.Manifest.Items
            .Select(item => (item.AnswerDecoderKey, item.AnswerDecoderVersion))
            .Distinct()
            .SingleOrDefault();
        if (decoderIdentity == default)
        {
            throw new InvalidOperationException("The execution manifest must select one answer decoder for quiz responses.");
        }

        var decoder = components.ResolveAnswerDecoder(
            request.Snapshot.AuthoringSource.ContentType,
            decoderIdentity.AnswerDecoderKey,
            decoderIdentity.AnswerDecoderVersion,
            request.Context);
        var algorithm = components.ResolveDeterministicAlgorithm(
            stage.AlgorithmKey,
            stage.AlgorithmVersion,
            request.Context);
        var projectedItems = request.Snapshot.Manifest.Items.Select(item =>
            request.Snapshot.ItemProjections.TryGetValue(item.ItemId, out var projection)
                ? projection
                : throw new InvalidOperationException($"Missing immutable projection for quiz item {item.ItemId}."))
            .ToArray();
        var normalizedResponse = decoder.Decode(request.Response);
        QuizAnswerDecoder.ValidateBindings(
            normalizedResponse,
            request.Snapshot.Manifest.Items.ToDictionary(item => item.ItemId, item => item.ItemType, StringComparer.Ordinal));
        return algorithm.EvaluateAsync(
            new DeterministicReviewRequest(projectedItems, request.Delivery, normalizedResponse),
            cancellationToken);
    }
}
