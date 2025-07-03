using Microsoft.AspNetCore.Mvc;
using GameGuild.Common.GraphQL.Authorization;
using GameGuild.Modules.Program.Interfaces;
using GameGuild.Modules.Program.Models;
using GameGuild.Common.Entities;
using GameGuild.Modules.Program.DTOs;

namespace GameGuild.Modules.Program.Controllers;

/// <summary>
/// REST API controller for ContentInteraction operations
/// Follows permission inheritance: ContentInteraction inherits permissions from Program
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ContentInteractionController : ControllerBase
{
    private readonly IContentInteractionService _contentInteractionService;
    private readonly IProgramContentService _programContentService;

    public ContentInteractionController(
        IContentInteractionService contentInteractionService,
        IProgramContentService programContentService)
    {
        _contentInteractionService = contentInteractionService;
        _programContentService = programContentService;
    }

    /// <summary>
    /// Start or resume content interaction
    /// Requires Read permission on the parent Program
    /// </summary>
    [HttpPost("start")]
    [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Read, "programId")]
    public async Task<ActionResult<ContentInteractionDto>> StartContent(
        [FromQuery] Guid programId,
        [FromBody] StartContentRequest request)
    {
        try
        {
            // Verify content belongs to the specified program
            var content = await _programContentService.GetContentByIdAsync(request.ContentId);
            if (content == null || content.ProgramId != programId)
            {
                return BadRequest("Content does not belong to the specified program.");
            }

            var interaction = await _contentInteractionService.StartContentAsync(
                request.ProgramUserId, 
                request.ContentId);

            return Ok(interaction.ToDto());
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update progress for a content interaction
    /// Requires Edit permission on the parent Program
    /// </summary>
    [HttpPut("{interactionId}/progress")]
    [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Edit, "programId")]
    public async Task<ActionResult<ContentInteractionDto>> UpdateProgress(
        [FromRoute] Guid interactionId,
        [FromQuery] Guid programId,
        [FromBody] UpdateProgressRequest request)
    {
        try
        {
            // Get the interaction to verify it belongs to the specified program
            var currentInteraction = await _contentInteractionService.GetInteractionAsync(request.ProgramUserId, request.ContentId);
            if (currentInteraction == null || currentInteraction.Content.ProgramId != programId)
            {
                return BadRequest("Interaction does not belong to the specified program.");
            }

            var interaction = await _contentInteractionService.UpdateProgressAsync(
                interactionId, 
                request.CompletionPercentage);

            return Ok(interaction.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Submit content interaction (makes it immutable)
    /// Requires Edit permission on the parent Program
    /// </summary>
    [HttpPost("{interactionId}/submit")]
    [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Edit, "programId")]
    public async Task<ActionResult<ContentInteractionDto>> SubmitContent(
        [FromRoute] Guid interactionId,
        [FromQuery] Guid programId,
        [FromBody] SubmitContentRequest request)
    {
        try
        {
            // Verify the interaction belongs to the specified program
            var currentInteraction = await _contentInteractionService.GetInteractionAsync(request.ProgramUserId, request.ContentId);
            if (currentInteraction == null || currentInteraction.Content.ProgramId != programId)
            {
                return BadRequest("Interaction does not belong to the specified program.");
            }

            var interaction = await _contentInteractionService.SubmitContentAsync(
                interactionId, 
                request.SubmissionData);

            return Ok(interaction.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Mark content as completed
    /// Requires Edit permission on the parent Program
    /// </summary>
    [HttpPost("{interactionId}/complete")]
    [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Edit, "programId")]
    public async Task<ActionResult<ContentInteractionDto>> CompleteContent(
        [FromRoute] Guid interactionId,
        [FromQuery] Guid programId,
        [FromBody] CompleteContentRequest request)
    {
        try
        {
            // Verify the interaction belongs to the specified program
            var currentInteraction = await _contentInteractionService.GetInteractionAsync(request.ProgramUserId, request.ContentId);
            if (currentInteraction == null || currentInteraction.Content.ProgramId != programId)
            {
                return BadRequest("Interaction does not belong to the specified program.");
            }

            var interaction = await _contentInteractionService.CompleteContentAsync(interactionId);

            return Ok(interaction.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Get interaction for specific user and content
    /// Requires Read permission on the parent Program
    /// </summary>
    [HttpGet("user/{programUserId}/content/{contentId}")]
    [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Read, "programId")]
    public async Task<ActionResult<ContentInteractionDto>> GetInteraction(
        [FromRoute] Guid programUserId,
        [FromRoute] Guid contentId,
        [FromQuery] Guid programId)
    {
        try
        {
            // Verify content belongs to the specified program
            var content = await _programContentService.GetContentByIdAsync(contentId);
            if (content == null || content.ProgramId != programId)
            {
                return BadRequest("Content does not belong to the specified program.");
            }

            var interaction = await _contentInteractionService.GetInteractionAsync(programUserId, contentId);

            if (interaction == null)
            {
                return NotFound("Interaction not found.");
            }

            return Ok(interaction.ToDto());
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Get all interactions for a user in a program
    /// Requires Read permission on the parent Program
    /// </summary>
    [HttpGet("user/{programUserId}")]
    [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Read, "programId")]
    public async Task<ActionResult<IEnumerable<ContentInteractionDto>>> GetUserInteractions(
        [FromRoute] Guid programUserId,
        [FromQuery] Guid programId)
    {
        try
        {
            var interactions = await _contentInteractionService.GetUserInteractionsAsync(programUserId);

            // Filter to only interactions for content in the specified program
            var filteredInteractions = interactions.Where(i => i.Content.ProgramId == programId);

            return Ok(filteredInteractions.ToDto());
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Update time spent on content
    /// Requires Edit permission on the parent Program
    /// </summary>
    [HttpPut("{interactionId}/time-spent")]
    [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Edit, "programId")]
    public async Task<ActionResult<ContentInteractionDto>> UpdateTimeSpent(
        [FromRoute] Guid interactionId,
        [FromQuery] Guid programId,
        [FromBody] UpdateTimeSpentRequest request)
    {
        try
        {
            // Verify the interaction belongs to the specified program
            var currentInteraction = await _contentInteractionService.GetInteractionAsync(request.ProgramUserId, request.ContentId);
            if (currentInteraction == null || currentInteraction.Content.ProgramId != programId)
            {
                return BadRequest("Interaction does not belong to the specified program.");
            }

            var interaction = await _contentInteractionService.UpdateTimeSpentAsync(
                interactionId, 
                request.AdditionalMinutes);

            return Ok(interaction.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}

/// <summary>
/// Request DTOs for ContentInteraction endpoints
/// </summary>
public record StartContentRequest(Guid ProgramUserId, Guid ContentId);

public record UpdateProgressRequest(Guid ProgramUserId, Guid ContentId, decimal CompletionPercentage);

public record SubmitContentRequest(Guid ProgramUserId, Guid ContentId, string SubmissionData);

public record CompleteContentRequest(Guid ProgramUserId, Guid ContentId);

public record UpdateTimeSpentRequest(Guid ProgramUserId, Guid ContentId, int AdditionalMinutes);
