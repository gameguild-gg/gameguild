using System.Text.Json;
using GameGuild.Learning.Assessments.Grading.Contracts;
using GameGuild.Learning.Assessments.Grading.Runtime;
using GameGuild.Learning.Grading.Contracts;

namespace GameGuild.Learning.Assessments.Grading.Abstractions;

public interface IAssessmentContentExtension
{
    string ContentType { get; }
}

public interface IAssessmentAuthoringProjectionEnricher : IAssessmentContentExtension
{
    ValueTask<AssessmentAuthoringProjectionV1> EnrichAsync(
        Assessment assessment,
        AssessmentAuthoringProjectionV1 projection,
        CancellationToken cancellationToken);
}

public interface IAssessmentSubmissionPayloadProjector : IAssessmentContentExtension
{
    void Project(
        AssessmentSubmission submission,
        AssessmentResponseEnvelopeV1 response);
}

public sealed record AssessmentInstructorReviewValidationV1(
    string? RuntimeRubricScoresCanonicalJson = null);

public sealed record AssessmentInstructorViewProjectionV1(
    JsonElement? Content = null,
    JsonElement? Rubric = null);

public interface IAssessmentInstructorReviewExtension : IAssessmentContentExtension
{
    AssessmentInstructorReviewValidationV1 ValidateResolution(
        AssessmentExecutionSnapshotV1 snapshot,
        InstructorReviewResolutionV1 resolution);

    string? ResolveFeedback(
        AssessmentExecutionSnapshotV1 snapshot,
        GradeResultV1? previousStageResult,
        string? itemId,
        string? suppliedFeedback);

    AssessmentInstructorViewProjectionV1 ProjectInstructorView(
        AssessmentExecutionSnapshotV1 snapshot);
}

public static class AssessmentContentExtensionResolver
{
    public static T? ResolveOptional<T>(
        IEnumerable<T> extensions,
        string contentType,
        string label)
        where T : IAssessmentContentExtension
    {
        ArgumentNullException.ThrowIfNull(extensions);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        var matches = extensions
            .Where(extension => string.Equals(extension.ContentType, contentType, StringComparison.Ordinal))
            .Take(2)
            .ToArray();
        return matches.Length switch
        {
            0 => default,
            1 => matches[0],
            _ => throw new InvalidOperationException(
                $"More than one {label} is registered for content type {contentType}."),
        };
    }
}
