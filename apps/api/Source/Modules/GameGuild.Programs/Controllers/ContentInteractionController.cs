

using Asp.Versioning;
using GameGuild.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Programs;

/// <summary>
/// REST API controller for ContentInteraction operations
/// Follows permission inheritance: ContentInteraction inherits permissions from Program
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/content-interactions")]
public class ContentInteractionController(IContentInteractionService contentInteractionService, IProgramContentService programContentService) : ControllerBase {
  /// <summary>
  /// Create or resume a content interaction
  /// Requires Read permission on the parent Program
  /// </summary>
  [HttpPost]
  // [GameGuild.Identity.Authorization.RequireResourcePermissionAttribute<ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<ContentInteractionDto>> CreateInteraction([FromQuery] Guid programId, [FromBody] StartContentRequest request) {
    try {
      // Verify content belongs to the specified program
      var content = await programContentService.GetContentByIdAsync(request.ContentId);

      if (content == null || content.ProgramId != programId) return BadRequest("Content does not belong to the specified program.");

      var interaction = await contentInteractionService.StartContentAsync(request.ProgramUserId, request.ContentId);

      return Ok(interaction.ToDto());
    }
    catch (Exception ex) { return BadRequest(ex.Message); }
  }

  /// <summary>
  /// Update progress for a content interaction
  /// Requires Edit permission on the parent Program
  /// </summary>
  [HttpPut("{interactionId}/progress")]
  // [GameGuild.Identity.Authorization.RequireResourcePermissionAttribute<ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult<ContentInteractionDto>> UpdateProgress([FromRoute] Guid interactionId, [FromQuery] Guid programId, [FromBody] UpdateProgressRequest request) {
    try {
      // Get the interaction to verify it belongs to the specified program
      var currentInteraction = await contentInteractionService.GetInteractionAsync(request.ProgramUserId, request.ContentId);

      if (currentInteraction == null || currentInteraction.Content.ProgramId != programId) return BadRequest("Interaction does not belong to the specified program.");

      var interaction = await contentInteractionService.UpdateProgressAsync(interactionId, request.CompletionPercentage);

      return Ok(interaction.ToDto());
    }
    catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    catch (Exception ex) { return StatusCode(500, ex.Message); }
  }

  /// <summary>
  /// Submit content interaction (makes it immutable)
  /// Requires Edit permission on the parent Program
  /// </summary>
  [HttpPost("{interactionId}:submit")]
  // [GameGuild.Identity.Authorization.RequireResourcePermissionAttribute<ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult<ContentInteractionDto>> SubmitContent([FromRoute] Guid interactionId, [FromQuery] Guid programId, [FromBody] SubmitContentRequest request) {
    try {
      // Verify the interaction belongs to the specified program
      var currentInteraction = await contentInteractionService.GetInteractionAsync(request.ProgramUserId, request.ContentId);

      if (currentInteraction == null || currentInteraction.Content.ProgramId != programId) return BadRequest("Interaction does not belong to the specified program.");

      var interaction = await contentInteractionService.SubmitContentAsync(interactionId, request.SubmissionData);

      return Ok(interaction.ToDto());
    }
    catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    catch (Exception ex) { return StatusCode(500, ex.Message); }
  }

  /// <summary>
  /// Mark content as completed
  /// Requires Edit permission on the parent Program
  /// </summary>
  [HttpPost("{interactionId}:complete")]
  // [GameGuild.Identity.Authorization.RequireResourcePermissionAttribute<ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult<ContentInteractionDto>> CompleteContent([FromRoute] Guid interactionId, [FromQuery] Guid programId, [FromBody] CompleteContentRequest request) {
    try {
      // Verify the interaction belongs to the specified program
      var currentInteraction = await contentInteractionService.GetInteractionAsync(request.ProgramUserId, request.ContentId);

      if (currentInteraction == null || currentInteraction.Content.ProgramId != programId) return BadRequest("Interaction does not belong to the specified program.");

      var interaction = await contentInteractionService.CompleteContentAsync(interactionId);

      return Ok(interaction.ToDto());
    }
    catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    catch (Exception ex) { return StatusCode(500, ex.Message); }
  }

  /// <summary>
  /// Get interaction for specific user and content
  /// Requires Read permission on the parent Program
  /// </summary>
  [HttpGet("user/{programUserId}/content/{contentId}")]
  // [GameGuild.Identity.Authorization.RequireResourcePermissionAttribute<ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<ContentInteractionDto>> GetInteraction([FromRoute] Guid programUserId, [FromRoute] Guid contentId, [FromQuery] Guid programId) {
    try {
      // Verify content belongs to the specified program
      var content = await programContentService.GetContentByIdAsync(contentId);

      if (content == null || content.ProgramId != programId) return BadRequest("Content does not belong to the specified program.");

      var interaction = await contentInteractionService.GetInteractionAsync(programUserId, contentId);

      if (interaction == null) return NotFound("Interaction not found.");

      return Ok(interaction.ToDto());
    }
    catch (Exception ex) { return StatusCode(500, ex.Message); }
  }

  /// <summary>
  /// Get all interactions for a user in a program
  /// Requires Read permission on the parent Program
  /// </summary>
  [HttpGet("user/{programUserId}")]
  // [GameGuild.Identity.Authorization.RequireResourcePermissionAttribute<ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<ContentInteractionDto>>> GetUserInteractions([FromRoute] Guid programUserId, [FromQuery] Guid programId) {
    try {
      var interactions = await contentInteractionService.GetUserInteractionsAsync(programUserId);

      // Filter to only interactions for content in the specified program
      var filteredInteractions = interactions.Where(i => i.Content.ProgramId == programId);

      return Ok(filteredInteractions.ToDto());
    }
    catch (Exception ex) { return StatusCode(500, ex.Message); }
  }

  /// <summary>
  /// Update time spent on content
  /// Requires Edit permission on the parent Program
  /// </summary>
  [HttpPut("{interactionId}/time-spent")]
  // [GameGuild.Identity.Authorization.RequireResourcePermissionAttribute<ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult<ContentInteractionDto>> UpdateTimeSpent([FromRoute] Guid interactionId, [FromQuery] Guid programId, [FromBody] UpdateTimeSpentRequest request) {
    try {
      // Verify the interaction belongs to the specified program
      var currentInteraction = await contentInteractionService.GetInteractionAsync(request.ProgramUserId, request.ContentId);

      if (currentInteraction == null || currentInteraction.Content.ProgramId != programId) return BadRequest("Interaction does not belong to the specified program.");

      var interaction = await contentInteractionService.UpdateTimeSpentAsync(interactionId, request.AdditionalMinutes);

      return Ok(interaction.ToDto());
    }
    catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    catch (Exception ex) { return StatusCode(500, ex.Message); }
  }
}
