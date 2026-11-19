using System.Text.Json;
using GameGuild.Learning.Assessments.Grading.Abstractions;
using GameGuild.Learning.Assessments.Grading.Contracts;
using GameGuild.Learning.Assessments.Grading.Runtime;
using GameGuild.Learning.Grading.Contracts;

namespace GameGuild.Learning.Assessments.CodeAdapter;

public sealed class CodeAssessmentAuthoringProjectionEnricher(IApplicationDbContext context)
    : IAssessmentAuthoringProjectionEnricher
{
    public string ContentType => CodeAssessmentContracts.ContentType;

    public async ValueTask<AssessmentAuthoringProjectionV1> EnrichAsync(
        Assessment assessment,
        AssessmentAuthoringProjectionV1 projection,
        CancellationToken cancellationToken) =>
        await CodeRubricSnapshot.MaterializeAsync(context, assessment, projection, cancellationToken)
            .ConfigureAwait(false);
}

public sealed class CodeAssessmentSubmissionPayloadProjector : IAssessmentSubmissionPayloadProjector
{
    public string ContentType => CodeAssessmentContracts.ContentType;

    public void Project(AssessmentSubmission submission, AssessmentResponseEnvelopeV1 response)
    {
        ArgumentNullException.ThrowIfNull(submission);
        submission.SetPayload(
            new SubmitAssessmentRequest
            {
                CodePayload = response.Payload.GetProperty("files").GetRawText(),
            },
            SubmissionModality.Code);
    }
}

public sealed class CodeAssessmentInstructorReviewExtension : IAssessmentInstructorReviewExtension
{
    public string ContentType => CodeAssessmentContracts.ContentType;

    public AssessmentInstructorReviewValidationV1 ValidateResolution(
        AssessmentExecutionSnapshotV1 snapshot,
        InstructorReviewResolutionV1 resolution) =>
        new(CodeRubricSnapshot.ValidateResolution(snapshot, resolution));

    public string? ResolveFeedback(
        AssessmentExecutionSnapshotV1 snapshot,
        GradeResultV1? previousStageResult,
        string? itemId,
        string? suppliedFeedback)
    {
        if (!string.IsNullOrWhiteSpace(suppliedFeedback))
        {
            return suppliedFeedback;
        }

        var previousFeedback = previousStageResult?.Items.SingleOrDefault(item =>
            item.ItemId == CodeAssessmentContracts.ItemId &&
            item.ReviewMethod == ReviewMethod.AutomatedReview &&
            item.HandlerKey == CodeAssessmentContracts.HandlerKey &&
            item.HandlerVersion == CodeAssessmentContracts.Version)?.Feedback;
        return previousFeedback ?? suppliedFeedback;
    }

    public AssessmentInstructorViewProjectionV1 ProjectInstructorView(
        AssessmentExecutionSnapshotV1 snapshot)
    {
        var projection = snapshot.ItemProjections[CodeAssessmentContracts.ItemId];
        return new AssessmentInstructorViewProjectionV1(
            projection.GetProperty("definition").Clone(),
            projection.TryGetProperty("rubric", out var rubric) ? rubric.Clone() : null);
    }
}
