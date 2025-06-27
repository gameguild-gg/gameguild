using GameGuild.Common.GraphQL.Authorization;
using GameGuild.Common.Entities;
using GameGuild.Modules.Program.Interfaces;
using GameGuild.Common.Enums;
using GameGuild.Modules.Program.Models;
using ProgramContentEntity = GameGuild.Modules.Program.Models.ProgramContent;


namespace GameGuild.Modules.Program.GraphQL;

/// <summary>
/// GraphQL queries for ProgramContent module
/// </summary>
[ExtendObjectType<GameGuild.Modules.User.GraphQL.Query>]
public class ProgramContentQueries {
  /// <summary>
  /// Gets a program content by its unique identifier (Resource Level: Read permission required for the parent Program)
  /// Layer 3: Resource Level - User needs Read permission on the parent Program that contains this content
  /// Note: The programId will be resolved from the content's ProgramId property
  /// </summary>
  [RequireResourcePermission<ProgramPermission, GameGuild.Modules.Program.Models.Program>(
    PermissionType.Read,
    "programId"
  )]
  public async Task<ProgramContentEntity?> GetProgramContentById(
    Guid id,
    [Service] IProgramContentService programContentService
  ) {
    return await programContentService.GetContentByIdAsync(id);
  }

  /// <summary>
  /// Gets all content for a specific program (Resource Level: Read permission required for the Program)
  /// Layer 3: Resource Level - User needs Read permission on the specific Program
  /// </summary>
  [RequireResourcePermission<ProgramPermission, GameGuild.Modules.Program.Models.Program>(
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
  /// Gets content by parent content ID (Content-Type Level: Read permission required for ProgramContent type)
  /// Layer 2: Content-Type Level - User needs Read permission on ProgramContent entity type
  /// </summary>
  [RequireContentTypePermission<ProgramContent>(PermissionType.Read)]
  public async Task<IEnumerable<ProgramContentEntity>> GetContentByParent(
    Guid parentContentId,
    [Service] IProgramContentService programContentService
  ) {
    return await programContentService.GetContentByParentAsync(parentContentId);
  }

  /// <summary>
  /// Gets root-level content for a program (Resource Level: Read permission required for the Program)
  /// Layer 3: Resource Level - User needs Read permission on the specific Program
  /// </summary>
  [RequireResourcePermission<ProgramPermission, GameGuild.Modules.Program.Models.Program>(
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
  [RequireResourcePermission<ProgramPermission, GameGuild.Modules.Program.Models.Program>(
    PermissionType.Read,
    "programId"
  )]
  public async Task<IEnumerable<ProgramContentEntity>> GetContentByType(
    Guid programId,
    GameGuild.Common.Enums.ProgramContentType contentType, [Service] IProgramContentService programContentService
  ) {
    return await programContentService.GetContentByTypeAsync(programId, contentType);
  }

  /// <summary>
  /// Gets content filtered by visibility (Resource Level: Read permission required for the Program)
  /// Layer 3: Resource Level - User needs Read permission on the specific Program
  /// </summary>
  [RequireResourcePermission<ProgramPermission, GameGuild.Modules.Program.Models.Program>(
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
  [RequireResourcePermission<ProgramPermission, GameGuild.Modules.Program.Models.Program>(
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
  [RequireResourcePermission<ProgramPermission, GameGuild.Modules.Program.Models.Program>(
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
  [RequireResourcePermission<ProgramPermission, GameGuild.Modules.Program.Models.Program>(
    PermissionType.Read,
    "programId"
  )]
  public async Task<int> GetContentCount(Guid programId, [Service] IProgramContentService programContentService) { return await programContentService.GetContentCountAsync(programId); }

  /// <summary>
  /// Gets required content count for a program (Resource Level: Read permission required for the Program)
  /// Layer 3: Resource Level - User needs Read permission on the specific Program
  /// </summary>
  [RequireResourcePermission<ProgramPermission, GameGuild.Modules.Program.Models.Program>(
    PermissionType.Read,
    "programId"
  )]
  public async Task<int> GetRequiredContentCount(Guid programId, [Service] IProgramContentService programContentService) { return await programContentService.GetRequiredContentCountAsync(programId); }
}
