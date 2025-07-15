using GameGuild.Common;
using GameGuild.Modules.Permissions;
using ProgramContentEntity = GameGuild.Modules.Programs.ProgramContent;


namespace GameGuild.Modules.Programs;

/// <summary>
/// GraphQL queries for ProgramContent module using CQRS pattern
/// </summary>
[ExtendObjectType<Query>]
public class ProgramContentQueries {
  /// <summary>
  /// Gets a program content by its unique identifier (Resource Level: Read permission required for the parent Program)
  /// Layer 3: Resource Level - User needs Read permission on the parent Program that contains this content
  /// </summary>
  [Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(
    PermissionType.Read,
    "programId"
  )]
  public async Task<ProgramContentEntity?> GetProgramContentById(
    Guid programId,
    Guid id,
    [Service] IProgramContentService programContentService
  ) {
    var content = await programContentService.GetContentByIdAsync(id);

    // Verify the content belongs to the specified program
    if (content != null && content.ProgramId != programId) return null; // Content doesn't belong to the specified program

    return content;
  }

  /// <summary>
  /// Gets all content for a specific program (Resource Level: Read permission required for the Program)
  /// Layer 3: Resource Level - User needs Read permission on the specific Program
  /// </summary>
  [Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(
    PermissionType.Read,
    "programId"
  )]
  public async Task<IEnumerable<ProgramContentEntity>> GetProgramContents(
    Guid programId,
    [Service] IProgramContentService programContentService
  ) {
    return await programContentService.GetContentByProgramAsync(programId);
  }

  /// <summary>
  /// Gets content by parent content ID (Resource Level: Read permission required for the parent Program)
  /// Layer 3: Resource Level - User needs Read permission on the parent Program that contains this content
  /// </summary>
  [Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(
    PermissionType.Read,
    "programId"
  )]
  public async Task<IEnumerable<ProgramContentEntity>> GetContentByParent(
    Guid programId,
    Guid parentContentId,
    [Service] IProgramContentService programContentService
  ) {
    var content = await programContentService.GetContentByParentAsync(parentContentId);

    // Filter to only return content that belongs to the specified program
    return content.Where(c => c.ProgramId == programId);
  }

  /// <summary>
  /// Gets root-level content for a program (Resource Level: Read permission required for the Program)
  /// Layer 3: Resource Level - User needs Read permission on the specific Program
  /// </summary>
  [Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(
    PermissionType.Read,
    "programId"
  )]
  public async Task<IEnumerable<ProgramContentEntity>> GetRootContent(
    Guid programId,
    [Service] IProgramContentService programContentService
  ) {
    return await programContentService.GetTopLevelContentAsync(programId);
  }

  /// <summary>
  /// Gets content filtered by type (Resource Level: Read permission required for the Program)
  /// Layer 3: Resource Level - User needs Read permission on the specific Program
  /// </summary>
  [Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(
    PermissionType.Read,
    "programId"
  )]
  public async Task<IEnumerable<ProgramContentEntity>> GetContentByType(
    Guid programId,
    Common.ProgramContentType contentType, [Service] IProgramContentService programContentService
  ) {
    return await programContentService.GetContentByTypeAsync(programId, contentType);
  }

  /// <summary>
  /// Gets content filtered by visibility (Resource Level: Read permission required for the Program)
  /// Layer 3: Resource Level - User needs Read permission on the specific Program
  /// </summary>
  [Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(
    PermissionType.Read,
    "programId"
  )]
  public async Task<IEnumerable<ProgramContentEntity>> GetContentByVisibility(
    Guid programId, Visibility visibility,
    [Service] IProgramContentService programContentService
  ) {
    return await programContentService.GetContentByVisibilityAsync(programId, visibility);
  }

  /// <summary>
  /// Gets required content for a program (Resource Level: Read permission required for the Program)
  /// Layer 3: Resource Level - User needs Read permission on the specific Program
  /// </summary>
  [Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(
    PermissionType.Read,
    "programId"
  )]
  public async Task<IEnumerable<ProgramContentEntity>> GetRequiredContent(
    Guid programId,
    [Service] IProgramContentService programContentService
  ) {
    return await programContentService.GetRequiredContentAsync(programId);
  }

  /// <summary>
  /// Searches program content (Resource Level: Read permission required for the Program)
  /// Layer 3: Resource Level - User needs Read permission on the specific Program
  /// </summary>
  [Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(
    PermissionType.Read,
    "programId"
  )]
  public async Task<IEnumerable<ProgramContentEntity>> SearchProgramContent(
    Guid programId, string searchTerm,
    [Service] IProgramContentService programContentService
  ) {
    return await programContentService.SearchContentAsync(programId, searchTerm);
  }

  /// <summary>
  /// Gets content count for a program (Resource Level: Read permission required for the Program)
  /// Layer 3: Resource Level - User needs Read permission on the specific Program
  /// </summary>
  [Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(
    PermissionType.Read,
    "programId"
  )]
  public async Task<int> GetContentCount(Guid programId, [Service] IProgramContentService programContentService) { return await programContentService.GetContentCountAsync(programId); }

  /// <summary>
  /// Gets required content count for a program (Resource Level: Read permission required for the Program)
  /// Layer 3: Resource Level - User needs Read permission on the specific Program
  /// </summary>
  [Common.Authorization.RequireResourcePermission<ProgramPermission, Program>(
    PermissionType.Read,
    "programId"
  )]
  public async Task<int> GetRequiredContentCount(Guid programId, [Service] IProgramContentService programContentService) { return await programContentService.GetRequiredContentCountAsync(programId); }
}
