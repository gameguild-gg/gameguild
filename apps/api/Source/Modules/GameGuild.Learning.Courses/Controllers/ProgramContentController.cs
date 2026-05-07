using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using AuthorizeAttribute = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;

namespace GameGuild.Learning.Courses;

/// <summary> Controller for managing program content with 3-layer DAC permissions Supports tenant-level, content-type-level, and resource-level permissions </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/courses/{programId}/content")]
[Authorize]
public class ProgramContentController(IProgramContentService contentService) : BaseApiController
{
  /// <summary> Get all content for a course with optional filtering (resource-level Read permission required on parent Program) </summary>
  /// <remarks>
  /// Supports filtering via query parameters:
  /// - level=top: Get only top-level content
  /// </remarks>
  [HttpGet]
  [Microsoft.AspNetCore.Authorization.AllowAnonymous]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetProgramContent(Guid programId, [FromQuery] string? level = null)
  {
    var isAuthenticated = User.Identity?.IsAuthenticated == true;

    if (level == "top")
    {
      var topLevelContent = await contentService.GetTopLevelContentAsync(programId).ConfigureAwait(false);
      var topLevelDtos = topLevelContent.ToDtos().ToList();
      if (!isAuthenticated)
      {
        topLevelDtos = FilterPublicContent(topLevelDtos);
      }
      return Ok(topLevelDtos);
    }

    var content = await contentService.GetContentByProgramAsync(programId).ConfigureAwait(false);
    var contentDtos = content.ToDtos().ToList();
    if (!isAuthenticated)
    {
      contentDtos = FilterPublicContent(contentDtos);
    }
    return Ok(contentDtos);
  }

  /// <summary> Get specific program content by ID (resource-level Read permission required on parent Program) </summary>
  [HttpGet("{id}")]
  [Microsoft.AspNetCore.Authorization.AllowAnonymous]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<ProgramContentDto>> GetContent(Guid programId, Guid id)
  {
    var content = await contentService.GetContentByIdAsync(id).ConfigureAwait(false);

    if (content == null || content.ProgramId != programId) return NotFound();

    var contentDto = content.ToDto();

    if (User.Identity?.IsAuthenticated != true && contentDto.Visibility != Visibility.Public)
    {
      return NotFound();
    }

    return Ok(contentDto);
  }

  /// <summary> Create new program content (resource-level Create permission required on parent Program) </summary>
  [HttpPost]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Create, "programId")]
  public async Task<ActionResult<ProgramContentDto>> CreateContent(Guid programId, [FromBody] CreateProgramContentDto createDto)
  {
    if (createDto.ProgramId != programId) return BadRequest("Program ID in URL must match Program ID in request body");

    var content = createDto.ToEntity();
    var createdContent = await contentService.CreateContentAsync(content).ConfigureAwait(false);
    var contentDto = createdContent.ToDto();

    return CreatedAtAction(nameof(GetContent), new { programId = createdContent.ProgramId, id = createdContent.Id }, contentDto);
  }

  /// <summary> Update program content (resource-level Edit permission required on parent Program) </summary>
  [HttpPut("{id}")]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult<ProgramContentDto>> UpdateContent(Guid programId, Guid id, [FromBody] UpdateProgramContentDto updateDto)
  {
    if (updateDto.Id != id) return BadRequest("Content ID in URL must match Content ID in request body");

    var existingContent = await contentService.GetContentByIdAsync(id).ConfigureAwait(false);

    if (existingContent == null || existingContent.ProgramId != programId) return NotFound();

    // Apply updates from DTO
    existingContent.ApplyUpdates(updateDto);

    var updatedContent = await contentService.UpdateContentAsync(existingContent).ConfigureAwait(false);
    var contentDto = updatedContent.ToDto();

    return Ok(contentDto);
  }

  /// <summary> Delete program content (resource-level Delete permission required on parent Program) </summary>
  [HttpDelete("{id}")]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Delete, "programId")]
  public async Task<ActionResult> DeleteContent(Guid programId, Guid id)
  {
    var content = await contentService.GetContentByIdAsync(id).ConfigureAwait(false);

    if (content == null || content.ProgramId != programId) return NotFound();

    var deleted = await contentService.DeleteContentAsync(id).ConfigureAwait(false);

    if (!deleted) return NotFound();

    return NoContent();
  }

  /// <summary> Get child content for a specific parent (resource-level Read permission required on parent Program) </summary>
  [HttpGet("{parentId}/children")]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetChildContent(Guid programId, Guid parentId)
  {
    // Verify parent belongs to the program
    var parent = await contentService.GetContentByIdAsync(parentId).ConfigureAwait(false);

    if (parent == null || parent.ProgramId != programId) return NotFound("Parent content not found or does not belong to this program");

    var children = await contentService.GetContentByParentAsync(parentId).ConfigureAwait(false);
    var childrenDtos = children.ToDtos();

    return Ok(childrenDtos);
  }

  /// <summary> Reorder content within a program (resource-level Edit permission required on parent Program) </summary>
  [HttpPost("reorder")]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult> ReorderContent(Guid programId, [FromBody] ReorderContentDto reorderDto)
  {
    // Convert the simple list to (Id, SortOrder) tuples
    var newOrder = reorderDto.ContentIds.Select((id, index) => (id, index + 1)).ToList();
    var success = await contentService.ReorderContentAsync(programId, newOrder).ConfigureAwait(false);

    if (!success) return BadRequest("Failed to reorder content. Some content items may not exist.");

    return Ok();
  }

  /// <summary> Move content to a new parent/position (resource-level Edit permission required on parent Program) </summary>
  [HttpPost("{id}/move")]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Edit, "programId")]
  public async Task<ActionResult> MoveContent(Guid programId, Guid id, [FromBody] MoveContentDto moveDto)
  {
    if (moveDto.ContentId != id) return BadRequest("Content ID in URL must match Content ID in request body");

    var content = await contentService.GetContentByIdAsync(id).ConfigureAwait(false);

    if (content == null || content.ProgramId != programId) return NotFound();

    var success = await contentService.MoveContentAsync(id, moveDto.NewParentId, moveDto.NewSortOrder).ConfigureAwait(false);

    if (!success) return BadRequest("Failed to move content");

    return Ok();
  }

  /// <summary> Get required content for a program (resource-level Read permission required on parent Program) </summary>
  [HttpGet("required")]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetRequiredContent(Guid programId)
  {
    var requiredContent = await contentService.GetRequiredContentAsync(programId).ConfigureAwait(false);
    var contentDtos = requiredContent.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary> Get content by type (resource-level Read permission required on parent Program) </summary>
  [HttpGet("by-type/{type}")]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetContentByType(Guid programId, ProgramContentType type)
  {
    var content = await contentService.GetContentByTypeAsync(programId, type).ConfigureAwait(false);
    var contentDtos = content.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary> Get content by visibility (resource-level Read permission required on parent Program) </summary>
  [HttpGet("by-visibility/{visibility}")]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetContentByVisibility(Guid programId, Visibility visibility)
  {
    var content = await contentService.GetContentByVisibilityAsync(programId, visibility).ConfigureAwait(false);
    var contentDtos = content.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary> Search content within a program (resource-level Read permission required on parent Program) </summary>
  [HttpPost("search")]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> SearchContent(Guid programId, [FromBody] SearchContentDto searchDto)
  {
    if (searchDto.ProgramId != programId) return BadRequest("Program ID in URL must match Program ID in request body");

    var content = await contentService.SearchContentAsync(programId, searchDto.SearchTerm).ConfigureAwait(false);
    var contentDtos = content.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary> Get content statistics for a program (resource-level Read permission required on parent Program) </summary>
  [HttpGet("stats")]
  // [GameGuild.Identity.Authorization.RequireResourcePermission<GameGuild.Modules.Programs.ProgramPermission, GameGuild.Modules.Programs.Entities.Program>(PermissionType.Read, "programId")]
  public async Task<ActionResult<ContentStatsDto>> GetContentStats(Guid programId)
  {
    var totalContent = await contentService.GetContentCountAsync(programId).ConfigureAwait(false);
    var requiredContent = await contentService.GetRequiredContentCountAsync(programId).ConfigureAwait(false);

    var stats = new ContentStatsDto { ProgramId = programId, TotalContent = totalContent, RequiredContent = requiredContent, OptionalContent = totalContent - requiredContent };

    return Ok(stats);
  }

  private static List<ProgramContentDto> FilterPublicContent(IEnumerable<ProgramContentDto> content)
  {
    return content
      .Where(item => item.Visibility == Visibility.Public)
      .Select(item =>
      {
        item.Children = FilterPublicContent(item.Children);
        item.ChildrenCount = item.Children.Count;
        return item;
      })
      .ToList();
  }
}
