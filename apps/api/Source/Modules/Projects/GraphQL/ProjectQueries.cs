using GameGuild.Modules.Contents;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Projects;

/// <summary>
/// GraphQL queries for Project module
/// </summary>
[ExtendObjectType<DbLoggerCategory.Query>]
public class ProjectQueries {
  /// <summary>
  /// Gets all projects accessible to the current user
  /// </summary>
  public async Task<IEnumerable<Project>> GetProjects([Service] IProjectService projectService) { return await projectService.GetAllProjectsAsync(); }

  /// <summary>
  /// Gets a project by its unique identifier
  /// </summary>
  public async Task<Project?> GetProjectById(Guid id, [Service] IProjectService projectService) { return await projectService.GetProjectByIdAsync(id); }

  /// <summary>
  /// Gets a project by its slug
  /// </summary>
  public async Task<Project?> GetProjectBySlug(string slug, [Service] IProjectService projectService) { return await projectService.GetProjectBySlugAsync(slug); }

  /// <summary>
  /// Gets projects by category
  /// </summary>
  public async Task<IEnumerable<Project>> GetProjectsByCategory(
    Guid categoryId,
    [Service] IProjectService projectService
  ) {
    return await projectService.GetProjectsByCategoryAsync(categoryId);
  }

  /// <summary>
  /// Gets projects by creator
  /// </summary>
  public async Task<IEnumerable<Project>> GetProjectsByCreator(
    Guid creatorId,
    [Service] IProjectService projectService
  ) {
    return await projectService.GetProjectsByCreatorAsync(creatorId);
  }

  /// <summary>
  /// Gets projects by status
  /// </summary>
  public async Task<IEnumerable<Project>> GetProjectsByStatus(
    ContentStatus status,
    [Service] IProjectService projectService
  ) {
    return await projectService.GetProjectsByStatusAsync(status);
  }

  /// <summary>
  /// Gets deleted projects (admin only)
  /// </summary>
  public async Task<IEnumerable<Project>> GetDeletedProjects([Service] IProjectService projectService) { return await projectService.GetDeletedProjectsAsync(); }
}
