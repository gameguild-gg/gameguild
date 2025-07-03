using GameGuild.Modules.Project.Services;
using GameGuild.Common.Entities;
using GameGuild.Common.Attributes;


namespace GameGuild.Modules.Project.GraphQL;

/// <summary>
/// GraphQL mutations for Project module
/// </summary>
[ExtendObjectType<GameGuild.Modules.User.GraphQL.Mutation>]
public class ProjectMutations {
  /// <summary>
  /// Creates a new project
  /// </summary>
  [RequireResourcePermission<Models.Project>(PermissionType.Create)]
  public async Task<Models.Project> CreateProject(CreateProjectInput input, [Service] IProjectService projectService) {
    var project = new Models.Project {
      Title = input.Title,
      Description = input.Description,
      ShortDescription = input.ShortDescription,
      WebsiteUrl = input.WebsiteUrl,
      RepositoryUrl = input.RepositoryUrl,
      SocialLinks = input.SocialLinks,
      CategoryId = input.CategoryId,
      Status = input.Status ?? ContentStatus.Draft,
      Visibility = input.Visibility ?? AccessLevel.Private,
    };

    return await projectService.CreateProjectAsync(project);
  }

  /// <summary>
  /// Updates an existing project
  /// </summary>
  [RequireResourcePermission<Models.Project>(PermissionType.Edit)]
  public async Task<Models.Project> UpdateProject(
    Guid id, UpdateProjectInput input,
    [Service] IProjectService projectService
  ) {
    var existingProject = await projectService.GetProjectByIdAsync(id);

    if (existingProject == null) throw new InvalidOperationException("Project not found");

    // Update only provided fields
    if (!string.IsNullOrEmpty(input.Title)) existingProject.Title = input.Title;

    if (input.Description != null) existingProject.Description = input.Description;

    if (input.ShortDescription != null) existingProject.ShortDescription = input.ShortDescription;

    if (input.WebsiteUrl != null) existingProject.WebsiteUrl = input.WebsiteUrl;

    if (input.RepositoryUrl != null) existingProject.RepositoryUrl = input.RepositoryUrl;

    if (input.SocialLinks != null) existingProject.SocialLinks = input.SocialLinks;

    if (input.CategoryId.HasValue) existingProject.CategoryId = input.CategoryId.Value;

    if (input.Status.HasValue) existingProject.Status = input.Status.Value;

    if (input.Visibility.HasValue) existingProject.Visibility = input.Visibility.Value;

    return await projectService.UpdateProjectAsync(existingProject);
  }

  /// <summary>
  /// Deletes a project (soft delete)
  /// </summary>
  [RequireResourcePermission<Models.Project>(PermissionType.Delete)]
  public async Task<bool> DeleteProject(Guid id, [Service] IProjectService projectService) { return await projectService.DeleteProjectAsync(id); }

  /// <summary>
  /// Restores a deleted project
  /// </summary>
  [RequireResourcePermission<Models.Project>(PermissionType.Restore)]
  public async Task<bool> RestoreProject(Guid id, [Service] IProjectService projectService) { return await projectService.RestoreProjectAsync(id); }
}

/// <summary>
/// Input type for creating a new project
/// </summary>
public record CreateProjectInput(
  string Title,
  string? Description,
  string? ShortDescription,
  string? WebsiteUrl,
  string? RepositoryUrl,
  string? SocialLinks,
  Guid CategoryId,
  string? Slug,
  ContentStatus? Status,
  AccessLevel? Visibility
) {
  private readonly string _title = Title;

  private readonly string? _description = Description;

  private readonly string? _shortDescription = ShortDescription;

  private readonly string? _websiteUrl = WebsiteUrl;

  private readonly string? _repositoryUrl = RepositoryUrl;

  private readonly string? _socialLinks = SocialLinks;

  private readonly Guid _categoryId = CategoryId;

  private readonly string? _slug = Slug;

  private readonly ContentStatus? _status = Status;

  private readonly AccessLevel? _visibility = Visibility;

  public string Title {
    get => _title;
    init => _title = value;
  }

  public string? Description {
    get => _description;
    init => _description = value;
  }

  public string? ShortDescription {
    get => _shortDescription;
    init => _shortDescription = value;
  }

  public string? WebsiteUrl {
    get => _websiteUrl;
    init => _websiteUrl = value;
  }

  public string? RepositoryUrl {
    get => _repositoryUrl;
    init => _repositoryUrl = value;
  }

  public string? SocialLinks {
    get => _socialLinks;
    init => _socialLinks = value;
  }

  public Guid CategoryId {
    get => _categoryId;
    init => _categoryId = value;
  }

  public string? Slug {
    get => _slug;
    init => _slug = value;
  }

  public ContentStatus? Status {
    get => _status;
    init => _status = value;
  }

  public AccessLevel? Visibility {
    get => _visibility;
    init => _visibility = value;
  }
}

/// <summary>
/// Input type for updating an existing project
/// </summary>
public record UpdateProjectInput(
  string? Title,
  string? Description,
  string? ShortDescription,
  string? WebsiteUrl,
  string? RepositoryUrl,
  string? SocialLinks,
  Guid? CategoryId,
  ContentStatus? Status,
  AccessLevel? Visibility
) {
  private readonly string? _title = Title;

  private readonly string? _description = Description;

  private readonly string? _shortDescription = ShortDescription;

  private readonly string? _websiteUrl = WebsiteUrl;

  private readonly string? _repositoryUrl = RepositoryUrl;

  private readonly string? _socialLinks = SocialLinks;

  private readonly Guid? _categoryId = CategoryId;

  private readonly ContentStatus? _status = Status;

  private readonly AccessLevel? _visibility = Visibility;

  public string? Title {
    get => _title;
    init => _title = value;
  }

  public string? Description {
    get => _description;
    init => _description = value;
  }

  public string? ShortDescription {
    get => _shortDescription;
    init => _shortDescription = value;
  }

  public string? WebsiteUrl {
    get => _websiteUrl;
    init => _websiteUrl = value;
  }

  public string? RepositoryUrl {
    get => _repositoryUrl;
    init => _repositoryUrl = value;
  }

  public string? SocialLinks {
    get => _socialLinks;
    init => _socialLinks = value;
  }

  public Guid? CategoryId {
    get => _categoryId;
    init => _categoryId = value;
  }

  public ContentStatus? Status {
    get => _status;
    init => _status = value;
  }

  public AccessLevel? Visibility {
    get => _visibility;
    init => _visibility = value;
  }
}
