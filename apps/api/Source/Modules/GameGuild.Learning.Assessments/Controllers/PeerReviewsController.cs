using Asp.Versioning;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Assessments;

/// <summary>
/// Student peer review endpoints. Claims are anonymous: responses never identify the reviewee.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/assessments")]
[Authorize]
public class PeerReviewsController : BaseApiController
{
    private readonly IPeerReviewAssignmentService _peerReviewService;
    private readonly IAssessmentService _assessmentService;
    private readonly IActorContextAccessor _actorContextAccessor;
    private readonly IProgramCrudService _programService;
    private readonly ILogger<PeerReviewsController> _logger;

    public PeerReviewsController(
        IPeerReviewAssignmentService peerReviewService,
        IAssessmentService assessmentService,
        IActorContextAccessor actorContextAccessor,
        IProgramCrudService programService,
        ILogger<PeerReviewsController> logger)
    {
        _peerReviewService = peerReviewService;
        _assessmentService = assessmentService;
        _actorContextAccessor = actorContextAccessor;
        _programService = programService;
        _logger = logger;
    }

    /// <summary>
    /// Claim the next peer review: a random submission among those tied for the fewest existing reviews.
    /// </summary>
    [HttpPost("{assessmentId:guid}/peer-reviews/claim")]
    public async Task<ActionResult<PeerReviewClaimDto>> ClaimPeerReview(Guid assessmentId)
    {
        var actor = _actorContextAccessor.ActorContext;
        if (actor.SubjectIdAsGuid == null)
        {
            return Unauthorized();
        }

        var assessment = await _assessmentService.GetAssessmentByIdAsync(assessmentId).ConfigureAwait(false);
        if (assessment == null) return NotFound();
        if (!await IsActorInProgramTenantAsync(assessment.CourseId).ConfigureAwait(false)) return Forbid();

        var result = await _peerReviewService
            .ClaimAsync(assessmentId, actor.SubjectIdAsGuid.Value)
            .ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "Peer review claim was rejected on assessment {AssessmentId}: {ErrorCode} {ErrorDescription}",
                assessmentId,
                result.Error.Code,
                result.Error.Description);
            return result.Error.Type == ErrorType.NotFound
                ? NotFound(result.Error)
                : BadRequest(new ProblemDetails
                {
                    Title = "Peer review claim rejected",
                    Detail = result.Error.Description
                });
        }

        return Ok(new PeerReviewClaimDto(result.Value.ReviewId, result.Value.MaskedSubmission));
    }

    private async Task<bool> IsActorInProgramTenantAsync(Guid courseId)
    {
        var actor = _actorContextAccessor.ActorContext;
        var program = await _programService.GetProgramByIdAsync(courseId).ConfigureAwait(false);
        if (program == null) return false;
        if (actor.IsSystemAdmin) return true;

        return actor.TenantId.HasValue &&
               (!program.TenantId.HasValue || program.TenantId == actor.TenantId);
    }
}

/// <summary>
/// Claim response. Deliberately carries no reviewee identity (no userId, no group id/name)
/// and no submission id — only the review to work on and its masked descriptor.
/// </summary>
public sealed record PeerReviewClaimDto(Guid ReviewId, string MaskedSubmission);
