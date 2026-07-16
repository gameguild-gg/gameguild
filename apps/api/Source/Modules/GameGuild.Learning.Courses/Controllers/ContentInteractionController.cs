using Asp.Versioning;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// REST API controller for ContentInteraction operations
/// Follows permission inheritance: ContentInteraction inherits permissions from Program
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/course-interactions")]
[Authorize]
public class ContentInteractionController(IContentInteractionService contentInteractionService, IProgramContentService programContentService, ILogger<ContentInteractionController> _logger) : BaseApiController {
  /// <summary>
  /// Create or resume a content interaction
  /// Requires Read permission on the parent Program
  /// </summary>
  [HttpPost]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<ContentInteractionDto>> CreateInteraction([FromQuery] Guid programId, [FromBody] StartContentRequest request) {
    _logger.LogDebug("Creating content interaction: ProgramId={ProgramId}, ContentId={ContentId}", programId, request.ContentId);

    // Verify content belongs to the specified program
    var content = await programContentService.GetContentByIdAsync(request.ContentId).ConfigureAwait(false);

    if (content == null || content.ProgramId != programId) return BadRequest("Content does not belong to the specified program.");

    var interaction = await contentInteractionService.StartContentAsync(request.ProgramUserId, request.ContentId).ConfigureAwait(false);

    return Ok(interaction.ToDto());
  }

  /// <summary>
  /// Update progress for a content interaction
  /// Requires Edit permission on the parent Program
  /// </summary>
  [HttpPut("{interactionId}/progress")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult<ContentInteractionDto>> UpdateProgress([FromRoute] Guid interactionId, [FromQuery] Guid programId, [FromBody] UpdateProgressRequest request) {
    try {
      // Get the interaction to verify it belongs to the specified program
      var currentInteraction = await contentInteractionService.GetInteractionAsync(request.ProgramUserId, request.ContentId).ConfigureAwait(false);

      if (currentInteraction == null || currentInteraction.Content.ProgramId != programId) return BadRequest("Interaction does not belong to the specified program.");

      var interaction = await contentInteractionService.UpdateProgressAsync(interactionId, request.CompletionPercentage).ConfigureAwait(false);

      return Ok(interaction.ToDto());
    }
    catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
  }

  /// <summary>
  /// Submit content interaction (makes it immutable)
  /// Requires Edit permission on the parent Program
  /// </summary>
  [HttpPost("{interactionId}/submit")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult<ContentInteractionDto>> SubmitContent([FromRoute] Guid interactionId, [FromQuery] Guid programId, [FromBody] SubmitContentRequest request) {
    try {
      // Verify the interaction belongs to the specified program
      var currentInteraction = await contentInteractionService.GetInteractionAsync(request.ProgramUserId, request.ContentId).ConfigureAwait(false);

      if (currentInteraction == null || currentInteraction.Content.ProgramId != programId) return BadRequest("Interaction does not belong to the specified program.");

      var interaction = await contentInteractionService.SubmitContentAsync(interactionId, request.SubmissionData).ConfigureAwait(false);

      return Ok(interaction.ToDto());
    }
    catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
  }

  /// <summary>
  /// Mark content as completed
  /// Requires Edit permission on the parent Program
  /// </summary>
  [HttpPost("{interactionId}/complete")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult<ContentInteractionDto>> CompleteContent([FromRoute] Guid interactionId, [FromQuery] Guid programId, [FromBody] CompleteContentRequest request) {
    try {
      // Verify the interaction belongs to the specified program
      var currentInteraction = await contentInteractionService.GetInteractionAsync(request.ProgramUserId, request.ContentId).ConfigureAwait(false);

      if (currentInteraction == null || currentInteraction.Content.ProgramId != programId) return BadRequest("Interaction does not belong to the specified program.");

      var interaction = await contentInteractionService.CompleteContentAsync(interactionId).ConfigureAwait(false);

      return Ok(interaction.ToDto());
    }
    catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
  }

  /// <summary>
  /// Get interaction for specific user and content
  /// Requires Read permission on the parent Program
  /// </summary>
  [HttpGet("user/{programUserId}/content/{contentId}")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<ContentInteractionDto>> GetInteraction([FromRoute] Guid programUserId, [FromRoute] Guid contentId, [FromQuery] Guid programId) {
    // Verify content belongs to the specified program
    var content = await programContentService.GetContentByIdAsync(contentId).ConfigureAwait(false);

    if (content == null || content.ProgramId != programId) return BadRequest("Content does not belong to the specified program.");

    var interaction = await contentInteractionService.GetInteractionAsync(programUserId, contentId).ConfigureAwait(false);

    if (interaction == null) return NotFound("Interaction not found.");

    return Ok(interaction.ToDto());
  }

  /// <summary>
  /// Get all interactions for a user in a program
  /// Requires Read permission on the parent Program
  /// </summary>
  [HttpGet("user/{programUserId}")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<ContentInteractionDto>>> GetUserInteractions([FromRoute] Guid programUserId, [FromQuery] Guid programId) {
    var interactions = await contentInteractionService.GetUserInteractionsAsync(programUserId).ConfigureAwait(false);

    // Filter to only interactions for content in the specified program
    var filteredInteractions = interactions.Where(i => i.Content.ProgramId == programId);

    return Ok(filteredInteractions.ToDto());
  }

  /// <summary>Get identity-free survey result records for course managers.</summary>
  [HttpGet("content/{contentId}/survey-results")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<SurveyResponseResultDto>>> GetSurveyResults([FromRoute] Guid contentId, [FromQuery] Guid programId) {
    var content = await programContentService.GetContentByIdAsync(contentId).ConfigureAwait(false);
    if (content is null || content.ProgramId != programId) return NotFound();

    try {
      return Ok(await contentInteractionService.GetSurveyResponsesAsync(contentId).ConfigureAwait(false));
    }
    catch (InvalidOperationException exception) {
      return BadRequest(exception.Message);
    }
  }

  /// <summary>
  /// Update time spent on content
  /// Requires Edit permission on the parent Program
  /// </summary>
  [HttpPut("{interactionId}/time-spent")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult<ContentInteractionDto>> UpdateTimeSpent([FromRoute] Guid interactionId, [FromQuery] Guid programId, [FromBody] UpdateTimeSpentRequest request) {
    try {
      // Verify the interaction belongs to the specified program
      var currentInteraction = await contentInteractionService.GetInteractionAsync(request.ProgramUserId, request.ContentId).ConfigureAwait(false);

      if (currentInteraction == null || currentInteraction.Content.ProgramId != programId) return BadRequest("Interaction does not belong to the specified program.");

      var interaction = await contentInteractionService.UpdateTimeSpentAsync(interactionId, request.AdditionalMinutes).ConfigureAwait(false);

      return Ok(interaction.ToDto());
    }
    catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
  }
}
