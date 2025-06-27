using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GameGuild.Modules.Program.Interfaces;
using GameGuild.Modules.Program.Models;
using GameGuild.Modules.Program.DTOs;
using GameGuild.Modules.Program.Extensions;
using GameGuild.Common.Attributes;
using GameGuild.Common.Entities;
using GameGuild.Common.Enums;


namespace GameGuild.Modules.Program.Controllers;

/// <summary>
/// Controller for managing program content with 3-layer DAC permissions
/// Supports tenant-level, content-type-level, and resource-level permissions
/// </summary>
[ApiController]
[Route("api/programs/{programId}/content")]
[Authorize]
public class ProgramContentController : ControllerBase {
  private readonly IProgramContentService _contentService;

  public ProgramContentController(IProgramContentService contentService) { _contentService = contentService; }

  /// <summary>
  /// Get all content for a program (content-type level Read permission required)
  /// </summary>
  [HttpGet]
  [RequireContentTypePermission<ProgramContent>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetProgramContent(Guid programId) {
    var content = await _contentService.GetContentByProgramAsync(programId);
    var contentDtos = content.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary>
  /// Get top-level content for a program (content-type level Read permission required)
  /// </summary>
  [HttpGet("top-level")]
  [RequireContentTypePermission<ProgramContent>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetTopLevelContent(Guid programId) {
    var content = await _contentService.GetTopLevelContentAsync(programId);
    var contentDtos = content.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary>
  /// Get specific program content by ID (resource-level Read permission required on parent Program)
  /// </summary>
  [HttpGet("{id}")]
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Read)]
  public async Task<ActionResult<ProgramContentDto>> GetContent(Guid programId, Guid id) {
    var content = await _contentService.GetContentByIdAsync(id);

    if (content == null || content.ProgramId != programId) { return NotFound(); }

    var contentDto = content.ToDto();

    return Ok(contentDto);
  }

  /// <summary>
  /// Create new program content (content-type level Create permission required)
  /// </summary>
  [HttpPost]
  [RequireContentTypePermission<ProgramContent>(PermissionType.Create)]
  public async Task<ActionResult<ProgramContentDto>> CreateContent(
    Guid programId,
    [FromBody] CreateProgramContentDto createDto
  ) {
    if (createDto.ProgramId != programId) { return BadRequest("Program ID in URL must match Program ID in request body"); }

    var content = createDto.ToEntity();
    var createdContent = await _contentService.CreateContentAsync(content);
    var contentDto = createdContent.ToDto();

    return CreatedAtAction(
      nameof(GetContent),
      new { programId = createdContent.ProgramId, id = createdContent.Id },
      contentDto
    );
  }

  /// <summary>
  /// Update program content (resource-level Edit permission required on parent Program)
  /// </summary>
  [HttpPut("{id}")]
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Edit)]
  public async Task<ActionResult<ProgramContentDto>> UpdateContent(
    Guid programId, Guid id,
    [FromBody] UpdateProgramContentDto updateDto
  ) {
    if (updateDto.Id != id) { return BadRequest("Content ID in URL must match Content ID in request body"); }

    var existingContent = await _contentService.GetContentByIdAsync(id);

    if (existingContent == null || existingContent.ProgramId != programId) { return NotFound(); }

    // Apply updates from DTO
    existingContent.ApplyUpdates(updateDto);

    var updatedContent = await _contentService.UpdateContentAsync(existingContent);
    var contentDto = updatedContent.ToDto();

    return Ok(contentDto);
  }

  /// <summary>
  /// Delete program content (resource-level Delete permission required on parent Program)
  /// </summary>
  [HttpDelete("{id}")]
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Delete)]
  public async Task<ActionResult> DeleteContent(Guid programId, Guid id) {
    var content = await _contentService.GetContentByIdAsync(id);

    if (content == null || content.ProgramId != programId) { return NotFound(); }

    var deleted = await _contentService.DeleteContentAsync(id);

    if (!deleted) { return NotFound(); }

    return NoContent();
  }

  /// <summary>
  /// Get child content for a specific parent (content-type level Read permission required)
  /// </summary>
  [HttpGet("{parentId}/children")]
  [RequireContentTypePermission<ProgramContent>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetChildContent(Guid programId, Guid parentId) {
    // Verify parent belongs to the program
    var parent = await _contentService.GetContentByIdAsync(parentId);

    if (parent == null || parent.ProgramId != programId) { return NotFound("Parent content not found or does not belong to this program"); }

    var children = await _contentService.GetContentByParentAsync(parentId);
    var childrenDtos = children.ToDtos();

    return Ok(childrenDtos);
  }

  /// <summary>
  /// Reorder content within a program (content-type level Edit permission required)
  /// </summary>
  [HttpPost("reorder")]
  [RequireContentTypePermission<ProgramContent>(PermissionType.Edit)]
  public async Task<ActionResult> ReorderContent(Guid programId, [FromBody] ReorderContentDto reorderDto) {
    // Convert the simple list to (Id, SortOrder) tuples
    var newOrder = reorderDto.ContentIds.Select((id, index) => (id, index + 1)).ToList();
    var success = await _contentService.ReorderContentAsync(programId, newOrder);

    if (!success) { return BadRequest("Failed to reorder content. Some content items may not exist."); }

    return Ok();
  }

  /// <summary>
  /// Move content to a new parent/position (resource-level Edit permission required on parent Program)
  /// </summary>
  [HttpPost("{id}/move")]
  [RequireResourcePermission<ProgramPermission, Models.Program>(PermissionType.Edit)]
  public async Task<ActionResult> MoveContent(Guid programId, Guid id, [FromBody] MoveContentDto moveDto) {
    if (moveDto.ContentId != id) { return BadRequest("Content ID in URL must match Content ID in request body"); }

    var content = await _contentService.GetContentByIdAsync(id);

    if (content == null || content.ProgramId != programId) { return NotFound(); }

    var success = await _contentService.MoveContentAsync(id, moveDto.NewParentId, moveDto.NewSortOrder);

    if (!success) { return BadRequest("Failed to move content"); }

    return Ok();
  }

  /// <summary>
  /// Get required content for a program (content-type level Read permission required)
  /// </summary>
  [HttpGet("required")]
  [RequireContentTypePermission<ProgramContent>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetRequiredContent(Guid programId) {
    var requiredContent = await _contentService.GetRequiredContentAsync(programId);
    var contentDtos = requiredContent.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary>
  /// Get content by type (content-type level Read permission required)
  /// </summary>
  [HttpGet("by-type/{type}")]
  [RequireContentTypePermission<ProgramContent>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetContentByType(
    Guid programId,
    ProgramContentType type
  ) {
    var content = await _contentService.GetContentByTypeAsync(programId, type);
    var contentDtos = content.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary>
  /// Get content by visibility (content-type level Read permission required)
  /// </summary>
  [HttpGet("by-visibility/{visibility}")]
  [RequireContentTypePermission<ProgramContent>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetContentByVisibility(
    Guid programId,
    Visibility visibility
  ) {
    var content = await _contentService.GetContentByVisibilityAsync(programId, visibility);
    var contentDtos = content.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary>
  /// Search content within a program (content-type level Read permission required)
  /// </summary>
  [HttpPost("search")]
  [RequireContentTypePermission<ProgramContent>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> SearchContent(
    Guid programId,
    [FromBody] SearchContentDto searchDto
  ) {
    if (searchDto.ProgramId != programId) { return BadRequest("Program ID in URL must match Program ID in request body"); }

    var content = await _contentService.SearchContentAsync(programId, searchDto.SearchTerm);
    var contentDtos = content.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary>
  /// Get content statistics for a program (content-type level Read permission required)
  /// </summary>
  [HttpGet("stats")]
  [RequireContentTypePermission<ProgramContent>(PermissionType.Read)]
  public async Task<ActionResult<ContentStatsDto>> GetContentStats(Guid programId) {
    var totalContent = await _contentService.GetContentCountAsync(programId);
    var requiredContent = await _contentService.GetRequiredContentCountAsync(programId);

    var stats = new ContentStatsDto { ProgramId = programId, TotalContent = totalContent, RequiredContent = requiredContent, OptionalContent = totalContent - requiredContent };

    return Ok(stats);
  }
}
