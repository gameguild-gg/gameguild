using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Assessments.Grading.Authoring;
using GameGuild.Learning.Assessments.Grading.Contracts;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Grading.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Assessments.CodeAdapter;

public sealed class CodeProgramContentPublicationParticipant(IApplicationDbContext context, CodeAssessmentTypeAdapter adapter, IAssessmentAuthoringService authoring, IActorContextAccessor actors) : IProgramContentPublicationParticipant
{
    private const string DefaultInstructorReviewConfiguration =
        "{\"schemaVersion\":1,\"instructor\":{\"requireOverrideReason\":false}}";

    public bool CanHandle(ProgramContent content) => content.Type == ProgramContentType.Code;
    public Task PreparePublishAsync(ProgramContent content, AuthoringContentPayload payload, Guid actorId) =>
        PreparePublishAsync(content, payload, actorId, CancellationToken.None);

    public async Task PreparePublishAsync(ProgramContent content, AuthoringContentPayload payload, Guid actorId, CancellationToken cancellationToken)
    {
        if (content.Type != ProgramContentType.Code || !payload.JsonBody.HasValue)
        {
            throw new ValidationException("Code content is required before publication.");
        }

        var actor = actors.ActorContext;
        if (!actor.IsAuthenticated || actor.SubjectIdAsGuid != actorId || !actor.TenantId.HasValue ||
            (content.TenantId.HasValue && content.TenantId != actor.TenantId))
        {
            throw new ValidationException("A matching tenant-scoped actor is required to publish Code assessments.");
        }

        var projection = adapter.ProjectAuthoring(payload.JsonBody.Value);
        var assessment = await context.Set<Assessment>()
            .SingleOrDefaultAsync(value => value.ContentId == content.Id && value.DeletedAt == null, cancellationToken)
            .ConfigureAwait(false);
        if (assessment is not null &&
            (assessment.CourseId != content.ProgramId || assessment.Type != AssessmentType.Assignment ||
             (assessment.TenantId.HasValue && assessment.TenantId != actor.TenantId)))
        {
            throw new ValidationException("The linked assessment does not match the Code content scope.");
        }

        if (assessment is null)
        {
            const ReviewMethods reviewMethods = ReviewMethods.AutomatedReview | ReviewMethods.InstructorReview;
            assessment = Assessment.Create(content.ProgramId, payload.Title, AssessmentType.Assignment,
                projection.MaxScore, payload.IsRequired, contentId: content.Id, reviewMethods: reviewMethods, slug: payload.Slug);
            assessment.TenantId = content.TenantId ?? actor.TenantId;
            assessment.SetDescription(payload.Description);
            assessment.SetDeliveryContract(SubmissionModality.Code, AssessmentPresentationMode.SingleStep);
            assessment.SetReviewPolicy(reviewMethods, DefaultInstructorReviewConfiguration,
                assessment.AttemptContributionMode, assessment.ContentCompletionMode, assessment.ResultReleaseMode, assessment.ResultReleaseScheduledFor);
            context.Set<Assessment>().Add(assessment);
            return;
        }

        assessment.SetMaxScore(projection.MaxScore);
        assessment.TenantId ??= actor.TenantId;
        if (assessment.ReviewMethods.HasFlag(ReviewMethods.InstructorReview) && string.IsNullOrWhiteSpace(assessment.ReviewConfigurationCanonicalJson))
        {
            assessment.SetReviewPolicy(assessment.ReviewMethods, DefaultInstructorReviewConfiguration, assessment.AttemptContributionMode, assessment.ContentCompletionMode, assessment.ResultReleaseMode, assessment.ResultReleaseScheduledFor);
        }

        context.Set<Assessment>().Update(assessment);
    }

    public Task FinalizePublishAsync(ProgramContent content, AuthoringContentPayload payload, Guid actorId) =>
        FinalizePublishAsync(content, payload, actorId, CancellationToken.None);

    public async Task FinalizePublishAsync(ProgramContent content, AuthoringContentPayload payload, Guid actorId, CancellationToken cancellationToken)
    {
        var assessment = await RequireAssessmentAsync(content.Id, cancellationToken).ConfigureAwait(false);
        var prepared = await authoring.PrepareAsync(assessment.Id, actorId, new PrepareAssessmentRevisionRequest(assessment.Version), cancellationToken).ConfigureAwait(false);
        if (!prepared.IsSuccess)
        {
            throw new ValidationException($"{prepared.Error.Code}: {prepared.Error.Description}");
        }

        var published = await authoring.PublishAsync(assessment.Id, actorId, new PublishAssessmentRevisionRequest(prepared.Value.RevisionId, assessment.Version), cancellationToken).ConfigureAwait(false);
        if (!published.IsSuccess)
        {
            throw new ValidationException($"{published.Error.Code}: {published.Error.Description}");
        }
    }

    private Task<Assessment> RequireAssessmentAsync(Guid contentId, CancellationToken cancellationToken) => context.Set<Assessment>().SingleAsync(value => value.ContentId == contentId && value.DeletedAt == null, cancellationToken);
}

public sealed class CodeProgramContentBoundary(CodeAssessmentTypeAdapter adapter) : IProgramContentLearnerProjector, IProgramContentAcademicMutationGuard
{
    public ProgramContentType ContentType => ProgramContentType.Code;

    public JsonElement Project(JsonElement authoringDocument)
    {
        var projection = adapter.ProjectAuthoring(authoringDocument).Items.Single().PrivateProjection;
        return adapter.GenerateDelivery(projection).GetProperty("definition").Clone();
    }

    public string? GetRejection(ProgramContent content, ProgramContentAcademicMutation mutation) => content.Type != ContentType || mutation == ProgramContentAcademicMutation.Authoring ? null : "Code assessment academic mutations must use the versioned assessment workflow.";
}
