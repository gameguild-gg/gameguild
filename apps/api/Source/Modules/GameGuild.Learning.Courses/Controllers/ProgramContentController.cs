using Asp.Versioning;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Utilities;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameGuild.Learning.Courses;

/// <summary> Controller for managing program content with 3-layer DAC permissions Supports tenant-level, content-type-level, and resource-level permissions </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/courses/{programId}/content")]
[Authorize]
public class ProgramContentController(
  IProgramContentService contentService,
  IProgramCrudService programService,
  IActorContextAccessor actorContextAccessor,
  IPermissionQueryService permissionQueryService) : BaseApiController
{
  /// <summary> Get all content for a course with optional filtering (resource-level Read permission required on parent Program) </summary>
  /// <remarks>
  /// Supports filtering via query parameters:
  /// - level=top: Get only top-level content
  /// </remarks>
  [HttpGet]
  [AllowAnonymous]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetProgramContent(Guid programId, [FromQuery] string? level = null)
  {
    var access = await ResolveContentAccessAsync(programId).ConfigureAwait(false);

    if (!access.CanViewFullContent && !access.CanViewPublicOutline)
    {
      return NotFound();
    }

    if (level == "top")
    {
      var topLevelContent = await contentService.GetTopLevelContentAsync(programId).ConfigureAwait(false);
      var topLevelDtos = topLevelContent.ToDtos().ToList();
      if (!access.CanViewFullContent)
      {
        topLevelDtos = SanitizePublicContent(topLevelDtos);
      }
      return Ok(topLevelDtos);
    }

    var content = await contentService.GetContentByProgramAsync(programId).ConfigureAwait(false);
    var contentDtos = content.ToDtos().ToList();
    if (!access.CanViewFullContent)
    {
      contentDtos = SanitizePublicContent(contentDtos);
    }
    return Ok(contentDtos);
  }

  /// <summary> Get specific program content by ID (resource-level Read permission required on parent Program) </summary>
  [HttpGet("{id}")]
  [AllowAnonymous]
  public async Task<ActionResult<ProgramContentDto>> GetContent(Guid programId, Guid id)
  {
    var access = await ResolveContentAccessAsync(programId).ConfigureAwait(false);
    if (!access.CanViewFullContent)
    {
      return NotFound();
    }

    var content = await contentService.GetContentByIdAsync(id).ConfigureAwait(false);

    if (content == null || content.ProgramId != programId) return NotFound();

    var contentDto = content.ToDto();

    return Ok(contentDto);
  }

  /// <summary>Submit work for the current learner on a course content item.</summary>
  [HttpPost("{id}/submit")]
  public async Task<ActionResult<ContentInteractionDto>> SubmitContent(Guid programId, Guid id, [FromBody] SubmitUserContentDto submitDto)
  {
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var currentUserId = GetCurrentUserId();
    if (currentUserId == null) return Unauthorized();
    if (!await HasStudentAccessAsync(programId, currentUserId.Value).ConfigureAwait(false)) return Forbid();

    var interaction = await programService.SubmitUserContentAsync(programId, currentUserId.Value, id, submitDto.SubmissionData).ConfigureAwait(false);

    if (interaction == null) return NotFound();

    return Ok(interaction.ToDto());
  }

  /// <summary> Create new program content (resource-level Create permission required on parent Program) </summary>
  [HttpPost]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Create, "programId")]
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
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit, "programId")]
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
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Delete, "programId")]
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
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetChildContent(Guid programId, Guid parentId)
  {
    if (!await HasFullCourseAccessAsync(programId).ConfigureAwait(false)) return NotFound();

    // Verify parent belongs to the program
    var parent = await contentService.GetContentByIdAsync(parentId).ConfigureAwait(false);

    if (parent == null || parent.ProgramId != programId) return NotFound("Parent content not found or does not belong to this program");

    var children = await contentService.GetContentByParentAsync(parentId).ConfigureAwait(false);
    var childrenDtos = children.ToDtos();

    return Ok(childrenDtos);
  }

  /// <summary> Reorder content within a program (resource-level Edit permission required on parent Program) </summary>
  [HttpPost("reorder")]
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit, "programId")]
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
  [RequireResourcePermission<PermissionType, Program>(PermissionType.Edit, "programId")]
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
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetRequiredContent(Guid programId)
  {
    if (!await HasFullCourseAccessAsync(programId).ConfigureAwait(false)) return NotFound();

    var requiredContent = await contentService.GetRequiredContentAsync(programId).ConfigureAwait(false);
    var contentDtos = requiredContent.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary> Get content by type (resource-level Read permission required on parent Program) </summary>
  [HttpGet("by-type/{type}")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetContentByType(Guid programId, ProgramContentType type)
  {
    if (!await HasFullCourseAccessAsync(programId).ConfigureAwait(false)) return NotFound();

    var content = await contentService.GetContentByTypeAsync(programId, type).ConfigureAwait(false);
    var contentDtos = content.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary> Get content by visibility (resource-level Read permission required on parent Program) </summary>
  [HttpGet("by-visibility/{visibility}")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> GetContentByVisibility(Guid programId, Visibility visibility)
  {
    if (!await HasFullCourseAccessAsync(programId).ConfigureAwait(false)) return NotFound();

    var content = await contentService.GetContentByVisibilityAsync(programId, visibility).ConfigureAwait(false);
    var contentDtos = content.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary> Search content within a program (resource-level Read permission required on parent Program) </summary>
  [HttpPost("search")]
  public async Task<ActionResult<IEnumerable<ProgramContentDto>>> SearchContent(Guid programId, [FromBody] SearchContentDto searchDto)
  {
    if (!await HasFullCourseAccessAsync(programId).ConfigureAwait(false)) return NotFound();
    if (searchDto.ProgramId != programId) return BadRequest("Program ID in URL must match Program ID in request body");

    var content = await contentService.SearchContentAsync(programId, searchDto.SearchTerm).ConfigureAwait(false);
    var contentDtos = content.ToDtos();

    return Ok(contentDtos);
  }

  /// <summary> Get content statistics for a program (resource-level Read permission required on parent Program) </summary>
  [HttpGet("stats")]
  public async Task<ActionResult<ContentStatsDto>> GetContentStats(Guid programId)
  {
    if (!await HasFullCourseAccessAsync(programId).ConfigureAwait(false)) return NotFound();

    var totalContent = await contentService.GetContentCountAsync(programId).ConfigureAwait(false);
    var requiredContent = await contentService.GetRequiredContentCountAsync(programId).ConfigureAwait(false);

    var stats = new ContentStatsDto { ProgramId = programId, TotalContent = totalContent, RequiredContent = requiredContent, OptionalContent = totalContent - requiredContent };

    return Ok(stats);
  }

  private async Task<ContentAccessResolution> ResolveContentAccessAsync(Guid programId)
  {
    var program = await programService.GetProgramByIdAsync(programId).ConfigureAwait(false);
    if (program == null)
    {
      return ContentAccessResolution.None;
    }

    var actor = actorContextAccessor.ActorContext;

    if (actor.IsSystemAdmin)
    {
      return new ContentAccessResolution(true, true);
    }

    var currentUserId = GetCurrentUserId();
    if (currentUserId.HasValue)
    {
      if (program.CreatorId == currentUserId.Value
          || await HasProgramManagementAccessAsync(programId, currentUserId.Value).ConfigureAwait(false))
      {
        return new ContentAccessResolution(true, true);
      }

      if (await HasStudentAccessAsync(programId, currentUserId.Value).ConfigureAwait(false))
      {
        return new ContentAccessResolution(true, true);
      }
    }

    var canViewPublicOutline = program.Status == ContentStatus.Published
        && program.Visibility == ContentVisibility.Public;

    return new ContentAccessResolution(false, canViewPublicOutline);
  }

  private async Task<bool> HasFullCourseAccessAsync(Guid programId)
  {
    var access = await ResolveContentAccessAsync(programId).ConfigureAwait(false);
    return access.CanViewFullContent;
  }

  private async Task<bool> HasStudentAccessAsync(Guid programId, Guid userId)
  {
    var progress = await programService.GetUserProgressDtoAsync(programId, userId).ConfigureAwait(false);
    return progress != null;
  }

  private async Task<bool> HasProgramManagementAccessAsync(Guid programId, Guid userId)
  {
    var tenantId = ClaimsExtractor.GetTenantIdAsGuid(User) ?? actorContextAccessor.ActorContext.TenantId;

    return await HasProgramPermissionAsync(userId, tenantId, programId, PermissionType.Read).ConfigureAwait(false)
      || await HasProgramPermissionAsync(userId, tenantId, programId, PermissionType.Edit).ConfigureAwait(false)
      || await HasProgramPermissionAsync(userId, tenantId, programId, PermissionType.Create).ConfigureAwait(false)
      || await HasProgramPermissionAsync(userId, tenantId, programId, PermissionType.Delete).ConfigureAwait(false);
  }

  private async Task<bool> HasProgramPermissionAsync(Guid userId, Guid? tenantId, Guid programId, PermissionType permission)
  {
    if (!tenantId.HasValue)
    {
      return false;
    }

    var permissionName = $"{nameof(Program)}.{programId}.{permission}";

    return await permissionQueryService.HasTenantPermissionAsync(
      userId,
      tenantId,
      permissionName).ConfigureAwait(false);
  }

  private static List<ProgramContentDto> SanitizePublicContent(IEnumerable<ProgramContentDto> content)
  {
    return content
      .Where(item => item.Visibility == Visibility.Public)
      .Select(item =>
      {
        item.Body = null;
        item.Children = SanitizePublicContent(item.Children);
        item.ChildrenCount = item.Children.Count;
        return item;
      })
      .ToList();
  }

  private sealed record ContentAccessResolution(bool CanViewFullContent, bool CanViewPublicOutline)
  {
    public static ContentAccessResolution None { get; } = new(false, false);
  }

  private Guid? GetCurrentUserId()
  {
    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirst("sub")?.Value
        ?? User.FindFirst("userId")?.Value;

    return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
  }
}
