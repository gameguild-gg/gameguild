using GameGuild.Common;
using GameGuild.Modules.Permissions;
using Microsoft.AspNetCore.Authorization;
using AuthorizeAttribute = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Controller for managing program content with 3-layer DAC permissions
/// Supports tenant-level, content-type-level, and resource-level permissions
/// </summary>
[ApiController]
[Route("api/programs/{programId}/content")]
[Authorize]
public class ProgramContentController(IProgramContentService contentService) : ControllerBase {
  /// <summary>
  /// Get all content for a program (resource-level Read permission required on parent Program)
  /// </summary>
  [HttpGet]
  [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetProgramContent(Guid programId) {
    var content = await contentService.GetContentByProgramAsync(programId);
    var contentDtos = content.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary>
  /// Get top-level content for a program (resource-level Read permission required on parent Program)
  /// </summary>
  [HttpGet("top-level")]
  [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetTopLevelContent(Guid programId) {
    var content = await contentService.GetTopLevelContentAsync(programId);
    var contentDtos = content.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary>
  /// Get specific program content by ID (resource-level Read permission required on parent Program)
  /// </summary>
  [HttpGet("{id}")]
  [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<ProgramContentDto>> GetContent(Guid programId, Guid id) {
    var content = await contentService.GetContentByIdAsync(id);

    if (content == null || content.ProgramId != programId) return NotFound();

    var contentDto = content.ToDto();

    return Ok(contentDto);
  }

  /// <summary>
  /// Create new program content (resource-level Create permission required on parent Program)
  /// </summary>
  [HttpPost]
  [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Create, "programId")]
  public async Task<ActionResult<ProgramContentDto>> CreateContent(
    Guid programId,
    [FromBody] CreateProgramContentDto createDto
  ) {
    if (createDto.ProgramId != programId) return BadRequest("Program ID in URL must match Program ID in request body");

    var content = createDto.ToEntity();
    var createdContent = await contentService.CreateContentAsync(content);
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
  [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult<ProgramContentDto>> UpdateContent(
    Guid programId, Guid id,
    [FromBody] UpdateProgramContentDto updateDto
  ) {
    if (updateDto.Id != id) return BadRequest("Content ID in URL must match Content ID in request body");

    var existingContent = await contentService.GetContentByIdAsync(id);

    if (existingContent == null || existingContent.ProgramId != programId) return NotFound();

    // Apply updates from DTO
    existingContent.ApplyUpdates(updateDto);

    var updatedContent = await contentService.UpdateContentAsync(existingContent);
    var contentDto = updatedContent.ToDto();

    return Ok(contentDto);
  }

  /// <summary>
  /// Delete program content (resource-level Delete permission required on parent Program)
  /// </summary>
  [HttpDelete("{id}")]
  [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Delete, "programId")]
  public async Task<ActionResult> DeleteContent(Guid programId, Guid id) {
    var content = await contentService.GetContentByIdAsync(id);

    if (content == null || content.ProgramId != programId) return NotFound();

    var deleted = await contentService.DeleteContentAsync(id);

    if (!deleted) return NotFound();

    return NoContent();
  }

  /// <summary>
  /// Get child content for a specific parent (resource-level Read permission required on parent Program)
  /// </summary>
  [HttpGet("{parentId}/children")]
  [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetChildContent(Guid programId, Guid parentId) {
    // Verify parent belongs to the program
    var parent = await contentService.GetContentByIdAsync(parentId);

    if (parent == null || parent.ProgramId != programId) return NotFound("Parent content not found or does not belong to this program");

    var children = await contentService.GetContentByParentAsync(parentId);
    var childrenDtos = children.ToDtos();

    return Ok(childrenDtos);
  }

  /// <summary>
  /// Reorder content within a program (resource-level Edit permission required on parent Program)
  /// </summary>
  [HttpPost("reorder")]
  [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult> ReorderContent(Guid programId, [FromBody] ReorderContentDto reorderDto) {
    // Convert the simple list to (Id, SortOrder) tuples
    var newOrder = reorderDto.ContentIds.Select((id, index) => (id, index + 1)).ToList();
    var success = await contentService.ReorderContentAsync(programId, newOrder);

    if (!success) return BadRequest("Failed to reorder content. Some content items may not exist.");

    return Ok();
  }

  /// <summary>
  /// Move content to a new parent/position (resource-level Edit permission required on parent Program)
  /// </summary>
  [HttpPost("{id}/move")]
  [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult> MoveContent(Guid programId, Guid id, [FromBody] MoveContentDto moveDto) {
    if (moveDto.ContentId != id) return BadRequest("Content ID in URL must match Content ID in request body");

    var content = await contentService.GetContentByIdAsync(id);

    if (content == null || content.ProgramId != programId) return NotFound();

    var success = await contentService.MoveContentAsync(id, moveDto.NewParentId, moveDto.NewSortOrder);

    if (!success) return BadRequest("Failed to move content");

    return Ok();
  }

  /// <summary>
  /// Get required content for a program (resource-level Read permission required on parent Program)
  /// </summary>
  [HttpGet("required")]
  [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetRequiredContent(Guid programId) {
    var requiredContent = await contentService.GetRequiredContentAsync(programId);
    var contentDtos = requiredContent.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary>
  /// Get content by type (resource-level Read permission required on parent Program)
  /// </summary>
  [HttpGet("by-type/{type}")]
  [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetContentByType(
    Guid programId,
    Common.ProgramContentType type
  ) {
    var content = await contentService.GetContentByTypeAsync(programId, type);
    var contentDtos = content.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary>
  /// Get content by visibility (resource-level Read permission required on parent Program)
  /// </summary>
  [HttpGet("by-visibility/{visibility}")]
  [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetContentByVisibility(
    Guid programId,
    Visibility visibility
  ) {
    var content = await contentService.GetContentByVisibilityAsync(programId, visibility);
    var contentDtos = content.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary>
  /// Search content within a program (resource-level Read permission required on parent Program)
  /// </summary>
  [HttpPost("search")]
  [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> SearchContent(
    Guid programId,
    [FromBody] SearchContentDto searchDto
  ) {
    if (searchDto.ProgramId != programId) return BadRequest("Program ID in URL must match Program ID in request body");

    var content = await contentService.SearchContentAsync(programId, searchDto.SearchTerm);
    var contentDtos = content.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary>
  /// Get content statistics for a program (resource-level Read permission required on parent Program)
  /// </summary>
  [HttpGet("stats")]
  [RequireResourcePermission<ProgramPermission, Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<ContentStatsDto>> GetContentStats(Guid programId) {
    var totalContent = await contentService.GetContentCountAsync(programId);
    var requiredContent = await contentService.GetRequiredContentCountAsync(programId);

    var stats = new ContentStatsDto { ProgramId = programId, TotalContent = totalContent, RequiredContent = requiredContent, OptionalContent = totalContent - requiredContent };

    return Ok(stats);
  }
}
