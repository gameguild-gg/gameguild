using GameGuild.Common;
using GameGuild.Modules.Contents;
using MediatR;


namespace GameGuild.Modules.Projects;

/// <summary>
/// GraphQL queries for Project module using CQRS pattern
/// </summary>
[ExtendObjectType<Query>]
public class ProjectQueries {
  /// <summary>
  /// Gets all projects accessible to the current user using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<Project>> Projects([Service] IMediator mediator, [Service] IProjectService projectService) { 
    // TODO: Replace with proper CQRS query when GetAllProjectsQuery is implemented
    // var query = new GetAllProjectsQuery();
    // return await mediator.Send(query);
    
    // Temporary fallback to service until CQRS queries are implemented
    return await projectService.GetAllProjectsAsync(); 
  }

  /// <summary>
  /// Gets a project by its unique identifier using CQRS pattern
  /// </summary>
  public async Task<Project?> ProjectById(Guid id, [Service] IMediator mediator, [Service] IProjectService projectService) { 
    // TODO: Replace with proper CQRS query when GetProjectByIdQuery is implemented
    // var query = new GetProjectByIdQuery { ProjectId = id };
    // return await mediator.Send(query);
    
    // Temporary fallback to service until CQRS queries are implemented
    return await projectService.GetProjectByIdAsync(id); 
  }

  /// <summary>
  /// Gets a project by its slug using CQRS pattern
  /// </summary>
  public async Task<Project?> GetProjectBySlug(string slug, [Service] IMediator mediator, [Service] IProjectService projectService) { 
    // TODO: Replace with proper CQRS query when GetProjectBySlugQuery is implemented
    return await projectService.GetProjectBySlugAsync(slug); 
  }

  /// <summary>
  /// Gets projects by category using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<Project>> GetProjectsByCategory(
    Guid categoryId,
    [Service] IMediator mediator,
    [Service] IProjectService projectService
  ) {
    // TODO: Replace with proper CQRS query when GetProjectsByCategoryQuery is implemented
    return await projectService.GetProjectsByCategoryAsync(categoryId);
  }

  /// <summary>
  /// Gets projects by creator using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<Project>> GetProjectsByCreator(
    Guid creatorId,
    [Service] IMediator mediator,
    [Service] IProjectService projectService
  ) {
    // TODO: Replace with proper CQRS query when GetProjectsByCreatorQuery is implemented
    return await projectService.GetProjectsByCreatorAsync(creatorId);
  }

  /// <summary>
  /// Gets projects by status using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<Project>> GetProjectsByStatus(
    ContentStatus status,
    [Service] IMediator mediator,
    [Service] IProjectService projectService
  ) {
    // TODO: Replace with proper CQRS query when GetProjectsByStatusQuery is implemented
    return await projectService.GetProjectsByStatusAsync(status);
  }

  /// <summary>
  /// Gets deleted projects (admin only) using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<Project>> GetDeletedProjects([Service] IMediator mediator, [Service] IProjectService projectService) { 
    // TODO: Replace with proper CQRS query when GetDeletedProjectsQuery is implemented
    return await projectService.GetDeletedProjectsAsync(); 
  }
}
