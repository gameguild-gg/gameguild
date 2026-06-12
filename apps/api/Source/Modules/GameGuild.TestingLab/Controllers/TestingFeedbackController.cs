using Asp.Versioning;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.TestingLab;

/// <summary>
/// Controller for feedback submission, reporting, quality rating, and statistics.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/testing")]
[Authorize]
public class TestingFeedbackController(
    ITestingFeedbackOperations feedbackService,
    IActorContextAccessor actorContextAccessor) : BaseApiController
{
    // POST: testing/requests/{requestId}/feedback
    [HttpPost("requests/{requestId}/feedback")]
    [RequireResourcePermission<PermissionType, TestingFeedback>(PermissionType.Create)]
    public async Task<ActionResult<TestingFeedback>> AddFeedback(Guid requestId, [FromBody] FeedbackRequest request)
    {
        var userId = actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (userId == null)
            return Unauthorized("User ID not found in token");

        var feedback = await feedbackService.AddFeedbackAsync(requestId, userId.Value, request.FeedbackFormId, request.FeedbackData, request.TestingContext, request.SessionId, request.AdditionalNotes).ConfigureAwait(false);
        return Ok(feedback);
    }

    // GET: testing/requests/{requestId}/feedback
    [HttpGet("requests/{requestId}/feedback")]
    [RequireResourcePermission<PermissionType, TestingFeedback>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<TestingFeedback>>> GetTestingRequestFeedback(Guid requestId)
    {
        var feedback = await feedbackService.GetTestingRequestFeedbackAsync(requestId).ConfigureAwait(false);
        return Ok(feedback);
    }

    // GET: testing/feedback/by-user/{userId}
    [HttpGet("feedback/by-user/{userId}")]
    [RequireResourcePermission<PermissionType, TestingFeedback>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<TestingFeedback>>> GetFeedbackByUser(Guid userId)
    {
        var feedback = await feedbackService.GetFeedbackByUserAsync(userId).ConfigureAwait(false);
        return Ok(feedback);
    }

    // POST: testing/feedback
    [HttpPost("feedback")]
    [RequireResourcePermission<PermissionType, TestingFeedback>(PermissionType.Create)]
    public async Task<ActionResult> SubmitFeedback(SubmitFeedbackDto feedbackDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (userId == null)
            return Unauthorized("User ID not found in token");

        await feedbackService.SubmitFeedbackAsync(feedbackDto, userId.Value).ConfigureAwait(false);

        return Ok(new { message = "Feedback submitted successfully" });
    }

    // POST: testing/feedback/{id}/report
    [HttpPost("feedback/{feedbackId}/report")]
    [RequireResourcePermission<PermissionType, TestingFeedback>(PermissionType.Report, "feedbackId")]
    public async Task<ActionResult> ReportFeedback(Guid feedbackId, ReportFeedbackDto reportDto)
    {
        var currentUserId = actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (currentUserId == null)
            return Unauthorized("User ID not found in token");

        await feedbackService.ReportFeedbackAsync(feedbackId, reportDto.Reason, currentUserId.Value).ConfigureAwait(false);

        return Ok(new { message = "Feedback reported successfully" });
    }

    // POST: testing/feedback/{id}/quality
    [HttpPost("feedback/{feedbackId}/quality")]
    [RequireResourcePermission<PermissionType, TestingFeedback>(PermissionType.Edit, "feedbackId")]
    public async Task<ActionResult> RateFeedbackQuality(Guid feedbackId, RateFeedbackQualityDto qualityDto)
    {
        var currentUserId = actorContextAccessor.ActorContext.SubjectIdAsGuid;
        if (currentUserId == null)
            return Unauthorized("User ID not found in token");

        await feedbackService.RateFeedbackQualityAsync(feedbackId, qualityDto.Quality, currentUserId.Value).ConfigureAwait(false);

        return Ok(new { message = "Feedback quality rated successfully" });
    }
}
