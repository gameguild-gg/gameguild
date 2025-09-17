using GameGuild.GraphQL;
using GameGuild.Modules.Contents;


namespace GameGuild.Modules.Projects;

/// <summary>
/// GraphQL queries for Project module using CQRS pattern
/// </summary>
[ExtendObjectType<Query>]
public class ProjectQueries {
  /// <summary>
  /// Gets all projects accessible to the current user using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<Project>> Projects([Service] CQRS.IMediator mediator, [Service] IProjectService projectService) {
    var query = new GetAllProjectsQuery();

    return await mediator.Send(query);
  }

  /// <summary>
  /// Gets a project by its unique identifier using CQRS pattern
  /// </summary>
  public async Task<Project?> ProjectById(Guid id, [Service] CQRS.IMediator mediator, [Service] IProjectService projectService) {
    var query = new GetProjectByIdQuery { ProjectId = id };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Gets a project by its slug using CQRS pattern
  /// </summary>
  public async Task<Project?> GetProjectBySlug(string slug, [Service] CQRS.IMediator mediator, [Service] IProjectService projectService) {
    var query = new GetProjectBySlugQuery { Slug = slug };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Gets projects by category using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<Project>> GetProjectsByCategory(
    Guid categoryId,
    [Service] CQRS.IMediator mediator,
    [Service] IProjectService projectService
  ) {
    var query = new GetProjectsByCategoryQuery { CategoryId = categoryId };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Gets projects by creator using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<Project>> GetProjectsByCreator(
    Guid creatorId,
    [Service] CQRS.IMediator mediator,
    [Service] IProjectService projectService
  ) {
    var query = new GetProjectsByCreatorQuery { CreatorId = creatorId };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Gets projects by status using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<Project>> GetProjectsByStatus(
    ContentStatus status,
    [Service] CQRS.IMediator mediator,
    [Service] IProjectService projectService
  ) {
    var query = new GetProjectsByStatusQuery { Status = status };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Gets deleted projects (admin only) using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<Project>> GetDeletedProjects([Service] CQRS.IMediator mediator, [Service] IProjectService projectService) {
    var query = new GetDeletedProjectsQuery();

    return await mediator.Send(query);
  }

  /// <summary>
  /// Search projects using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<Project>> SearchProjects(
    string searchTerm,
    [Service] CQRS.IMediator mediator,
    ProjectType? type = null,
    Guid? categoryId = null,
    ContentStatus? status = null,
    int skip = 0,
    int take = 50
  ) {
    var query = new SearchProjectsQuery {
      SearchTerm = searchTerm,
      Type = type,
      CategoryId = categoryId,
      Status = status,
      Skip = skip,
      Take = take,
    };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Gets popular projects using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<Project>> GetPopularProjects(
    [Service] CQRS.IMediator mediator,
    ProjectType? type = null,
    int take = 10
  ) {
    var query = new GetPopularProjectsQuery { Type = type, Take = take };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Gets recent projects using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<Project>> GetRecentProjects(
    [Service] CQRS.IMediator mediator,
    ProjectType? type = null,
    int take = 10
  ) {
    var query = new GetRecentProjectsQuery { Type = type, Take = take };

    return await mediator.Send(query);
  }

  /// <summary>
  /// Gets featured projects using CQRS pattern
  /// </summary>
  public async Task<IEnumerable<Project>> GetFeaturedProjects(
    [Service] CQRS.IMediator mediator,
    ProjectType? type = null,
    int take = 10
  ) {
    var query = new GetFeaturedProjectsQuery { Type = type, Take = take };

    return await mediator.Send(query);
  }
}
