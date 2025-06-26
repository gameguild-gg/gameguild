using GameGuild.Modules.Project.Services;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Project.GraphQL;

/// <summary>
/// GraphQL queries for Project module
/// </summary>
[ExtendObjectType<GameGuild.Modules.User.GraphQL.Query>]
public class ProjectQueries
{
    /// <summary>
    /// Gets all projects accessible to the current user
    /// </summary>
    public async Task<IEnumerable<Models.Project>> GetProjects(
        [Service] IProjectService projectService)
    {
        return await projectService.GetAllProjectsAsync();
    }

    /// <summary>
    /// Gets a project by its unique identifier
    /// </summary>
    public async Task<Models.Project?> GetProjectById(
        Guid id,
        [Service] IProjectService projectService)
    {
        return await projectService.GetProjectByIdAsync(id);
    }

    /// <summary>
    /// Gets a project by its slug
    /// </summary>
    public async Task<Models.Project?> GetProjectBySlug(
        string slug,
        [Service] IProjectService projectService)
    {
        return await projectService.GetProjectBySlugAsync(slug);
    }

    /// <summary>
    /// Gets projects by category
    /// </summary>
    public async Task<IEnumerable<Models.Project>> GetProjectsByCategory(
        Guid categoryId,
        [Service] IProjectService projectService)
    {
        return await projectService.GetProjectsByCategoryAsync(categoryId);
    }

    /// <summary>
    /// Gets projects by creator
    /// </summary>
    public async Task<IEnumerable<Models.Project>> GetProjectsByCreator(
        Guid creatorId,
        [Service] IProjectService projectService)
    {
        return await projectService.GetProjectsByCreatorAsync(creatorId);
    }

    /// <summary>
    /// Gets projects by status
    /// </summary>
    public async Task<IEnumerable<Models.Project>> GetProjectsByStatus(
        ContentStatus status,
        [Service] IProjectService projectService)
    {
        return await projectService.GetProjectsByStatusAsync(status);
    }

    /// <summary>
    /// Gets deleted projects (admin only)
    /// </summary>
    public async Task<IEnumerable<Models.Project>> GetDeletedProjects(
        [Service] IProjectService projectService)
    {
        return await projectService.GetDeletedProjectsAsync();
    }
}
