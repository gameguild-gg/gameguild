


using Asp.Versioning;
using GameGuild.Enums;
using Microsoft.AspNetCore.Mvc;
using AuthorizeAttribute = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;

namespace GameGuild.Programs;

/// <summary> Controller for managing program content with 3-layer DAC permissions Supports tenant-level, content-type-level, and resource-level permissions </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/programs/{programId}/content")]
[Authorize]
public class ProgramContentController(IProgramContentService contentService) : ControllerBase {
  /// <summary> Get all content for a program with optional filtering (resource-level Read permission required on parent Program) </summary>
  /// <remarks>
  /// Supports filtering via query parameters:
  /// - level=top: Get only top-level content
  /// - required=true: Get only required content
  /// - type={type}: Filter by content type
  /// - visibility={visibility}: Filter by visibility
  /// </remarks>
  [HttpGet]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetProgramContent(
      Guid programId,
      [FromQuery] string? level = null,
      [FromQuery] bool? required = null,
      [FromQuery] ProgramContentType? type = null,
      [FromQuery] Visibility? visibility = null) {
    // Handle top-level filter
    if (level == "top") {
      var topLevelContent = await contentService.GetTopLevelContentAsync(programId);
      return Ok(topLevelContent.ToDtos());
    }

    // Handle required filter
    if (required == true) {
      var requiredContent = await contentService.GetRequiredContentAsync(programId);
      return Ok(requiredContent.ToDtos());
    }

    // Handle content type filter
    if (type.HasValue) {
      var typeContent = await contentService.GetContentByTypeAsync(programId, type.Value);
      return Ok(typeContent.ToDtos());
    }

    // Handle visibility filter
    if (visibility.HasValue) {
      var visibilityContent = await contentService.GetContentByVisibilityAsync(programId, visibility.Value);
      return Ok(visibilityContent.ToDtos());
    }

    // Default: return all content
    var content = await contentService.GetContentByProgramAsync(programId);
    var contentDtos = content.ToDtos();
    return Ok(contentDtos);
  }

  /// <summary> Get specific program content by ID (resource-level Read permission required on parent Program) </summary>
  [HttpGet("{id}")]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<ProgramContentDto>> GetContent(Guid programId, Guid id) {
    var content = await contentService.GetContentByIdAsync(id);

    if (content == null || content.ProgramId != programId) return NotFound();

    var contentDto = content.ToDto();

    return Ok(contentDto);
  }

  /// <summary> Create new program content (resource-level Create permission required on parent Program) </summary>
  [HttpPost]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Create, "programId")]
  public async Task<ActionResult<ProgramContentDto>> CreateContent(Guid programId, [FromBody] CreateProgramContentDto createDto) {
    if (createDto.ProgramId != programId) return BadRequest("Program ID in URL must match Program ID in request body");

    var content = createDto.ToEntity();
    var createdContent = await contentService.CreateContentAsync(content);
    var contentDto = createdContent.ToDto();

    return CreatedAtAction(nameof(GetContent), new { programId = createdContent.ProgramId, id = createdContent.Id }, contentDto);
  }

  /// <summary> Update program content (resource-level Edit permission required on parent Program) </summary>
  [HttpPut("{id}")]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult<ProgramContentDto>> UpdateContent(Guid programId, Guid id, [FromBody] UpdateProgramContentDto updateDto) {
    if (updateDto.Id != id) return BadRequest("Content ID in URL must match Content ID in request body");

    var existingContent = await contentService.GetContentByIdAsync(id);

    if (existingContent == null || existingContent.ProgramId != programId) return NotFound();

    // Apply updates from DTO
    existingContent.ApplyUpdates(updateDto);

    var updatedContent = await contentService.UpdateContentAsync(existingContent);
    var contentDto = updatedContent.ToDto();

    return Ok(contentDto);
  }

  /// <summary> Delete program content (resource-level Delete permission required on parent Program) </summary>
  [HttpDelete("{id}")]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Delete, "programId")]
  public async Task<ActionResult> DeleteContent(Guid programId, Guid id) {
    var content = await contentService.GetContentByIdAsync(id);

    if (content == null || content.ProgramId != programId) return NotFound();

    var deleted = await contentService.DeleteContentAsync(id);

    if (!deleted) return NotFound();

    return NoContent();
  }

  /// <summary> Get child content for a specific parent (resource-level Read permission required on parent Program) </summary>
  [HttpGet("{parentId}/children")]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetChildContent(Guid programId, Guid parentId) {
    // Verify parent belongs to the program
    var parent = await contentService.GetContentByIdAsync(parentId);

    if (parent == null || parent.ProgramId != programId) return NotFound("Parent content not found or does not belong to this program");

    var children = await contentService.GetContentByParentAsync(parentId);
    var childrenDtos = children.ToDtos();

    return Ok(childrenDtos);
  }

  /// <summary> Reorder content within a program (resource-level Edit permission required on parent Program) </summary>
  [HttpPost(":reorder")]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult> ReorderContent(Guid programId, [FromBody] ReorderContentDto reorderDto) {
    // Convert the simple list to (Id, SortOrder) tuples
    var newOrder = reorderDto.ContentIds.Select((id, index) => (id, index + 1)).ToList();
    var success = await contentService.ReorderContentAsync(programId, newOrder);

    if (!success) return BadRequest("Failed to reorder content. Some content items may not exist.");

    return Ok();
  }

  /// <summary> Move content to a new parent/position (resource-level Edit permission required on parent Program) </summary>
  [HttpPost("{id}:move")]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult> MoveContent(Guid programId, Guid id, [FromBody] MoveContentDto moveDto) {
    if (moveDto.ContentId != id) return BadRequest("Content ID in URL must match Content ID in request body");

    var content = await contentService.GetContentByIdAsync(id);

    if (content == null || content.ProgramId != programId) return NotFound();

    var success = await contentService.MoveContentAsync(id, moveDto.NewParentId, moveDto.NewSortOrder);

    if (!success) return BadRequest("Failed to move content");

    return Ok();
  }

  /// <summary> Search content within a program (resource-level Read permission required on parent Program) </summary>
  [HttpPost(":search")]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> SearchContent(Guid programId, [FromBody] SearchContentDto searchDto) {
    if (searchDto.ProgramId != programId) return BadRequest("Program ID in URL must match Program ID in request body");

    var content = await contentService.SearchContentAsync(programId, searchDto.SearchTerm);
    var contentDtos = content.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary> Get content statistics for a program (resource-level Read permission required on parent Program) </summary>
  [HttpGet("stats")]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<ContentStatsDto>> GetContentStats(Guid programId) {
    var totalContent = await contentService.GetContentCountAsync(programId);
    var requiredContent = await contentService.GetRequiredContentCountAsync(programId);

    var stats = new ContentStatsDto { ProgramId = programId, TotalContent = totalContent, RequiredContent = requiredContent, OptionalContent = totalContent - requiredContent };

    return Ok(stats);
  }
}
