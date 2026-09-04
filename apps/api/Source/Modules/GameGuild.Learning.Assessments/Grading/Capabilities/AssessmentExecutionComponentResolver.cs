using GameGuild.Learning.Assessments.Grading.Abstractions;
using GameGuild.Learning.Assessments.Grading.Contracts;

namespace GameGuild.Learning.Assessments.Grading.Capabilities;

public sealed class AssessmentExecutionComponentResolver(
    IReviewCapabilityRegistry capabilities,
    IEnumerable<IAssessmentItemProjector> projectors,
    IEnumerable<IAssessmentDeliveryGenerator> deliveryGenerators,
    IEnumerable<IAssessmentAnswerDecoder> answerDecoders,
    IEnumerable<IDeterministicReviewAlgorithm> deterministicAlgorithms) : IAssessmentExecutionComponentResolver
{
    public IAssessmentItemProjector ResolveItemProjector(
        string contentType,
        string key,
        string version,
        ReviewExecutionContext context)
    {
        RequireCapability(ExecutableComponentKind.ItemProjector, key, version, context);
        return ResolveSingle(
            projectors,
            candidate => Same(candidate.ContentType, contentType) && Same(candidate.Key, key) && Same(candidate.Version, version),
            $"item projector {key}@{version} for {contentType}");
    }

    public IAssessmentDeliveryGenerator ResolveDeliveryGenerator(
        string contentType,
        string key,
        string version,
        ReviewExecutionContext context)
    {
        RequireCapability(ExecutableComponentKind.DeliveryGenerator, key, version, context);
        return ResolveSingle(
            deliveryGenerators,
            candidate => Same(candidate.ContentType, contentType) && Same(candidate.Key, key) && Same(candidate.Version, version),
            $"delivery generator {key}@{version} for {contentType}");
    }

    public IAssessmentAnswerDecoder ResolveAnswerDecoder(
        string contentType,
        string key,
        string version,
        ReviewExecutionContext context)
    {
        RequireCapability(ExecutableComponentKind.AnswerDecoder, key, version, context);
        return ResolveSingle(
            answerDecoders,
            candidate => Same(candidate.ContentType, contentType) && Same(candidate.Key, key) && Same(candidate.Version, version),
            $"answer decoder {key}@{version} for {contentType}");
    }

    public IDeterministicReviewAlgorithm ResolveDeterministicAlgorithm(
        string key,
        string version,
        ReviewExecutionContext context)
    {
        RequireCapability(ExecutableComponentKind.GradingAlgorithm, key, version, context);
        return ResolveSingle(
            deterministicAlgorithms,
            candidate => Same(candidate.Key, key) && Same(candidate.Version, version),
            $"deterministic review algorithm {key}@{version}");
    }

    private void RequireCapability(
        ExecutableComponentKind kind,
        string key,
        string version,
        ReviewExecutionContext context)
    {
        if (capabilities.Resolve(kind, key, version, context) is null)
        {
            throw Unavailable($"{kind} {key}@{version}", context);
        }
    }

    internal static T ResolveSingle<T>(IEnumerable<T> candidates, Func<T, bool> predicate, string label)
    {
        var matches = candidates.Where(predicate).Take(2).ToArray();
        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException($"The manifest references an unregistered {label}."),
            _ => throw new InvalidOperationException($"More than one implementation is registered for {label}."),
        };
    }

    internal static bool Same(string left, string right) => string.Equals(left, right, StringComparison.Ordinal);

    internal static InvalidOperationException Unavailable(string component, ReviewExecutionContext context) =>
        new($"The manifest references {component}, which is unavailable for {context}.");
}

public sealed class ReviewStageHandlerResolver(
    IReviewCapabilityRegistry capabilities,
    IEnumerable<IReviewStageHandler> handlers) : IReviewStageHandlerResolver
{
    public IReviewStageHandler Resolve(
        ReviewMethod method,
        string key,
        string version,
        ReviewExecutionContext context)
    {
        if (capabilities.ResolveReview(method, key, version, context) is null)
        {
            throw AssessmentExecutionComponentResolver.Unavailable(
                $"review handler {method}:{key}@{version}",
                context);
        }

        return AssessmentExecutionComponentResolver.ResolveSingle(
            handlers,
            candidate => candidate.Method == method &&
                         AssessmentExecutionComponentResolver.Same(candidate.Key, key) &&
                         AssessmentExecutionComponentResolver.Same(candidate.Version, version) &&
                         candidate.Contexts.Contains(context),
            $"review handler {method}:{key}@{version}");
    }
}
