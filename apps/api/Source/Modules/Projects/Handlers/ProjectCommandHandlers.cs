using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Permissions;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Projects;

/// <summary>
/// Role constants for project collaborators
/// </summary>
public static class ProjectRoles {
  public const string Owner = "Owner";
  public const string Editor = "Editor";
  public const string Viewer = "Viewer";
}

/// <summary>
/// Command handlers for project operations
/// </summary>
public class ProjectCommandHandlers :
  IRequestHandler<CreateProjectCommand, CreateProjectResult>,
  IRequestHandler<UpdateProjectCommand, UpdateProjectResult>,
  IRequestHandler<DeleteProjectCommand, DeleteProjectResult>,
  IRequestHandler<PublishProjectCommand, PublishProjectResult>,
  IRequestHandler<UnpublishProjectCommand, UnpublishProjectResult>,
  IRequestHandler<ArchiveProjectCommand, ArchiveProjectResult> {
  private readonly ApplicationDbContext _context;
  private readonly IUserContext _userContext;
  private readonly ITenantContext _tenantContext;
  private readonly ILogger<ProjectCommandHandlers> _logger;

  public ProjectCommandHandlers(
    ApplicationDbContext context,
    IUserContext userContext,
    ITenantContext tenantContext,
    ILogger<ProjectCommandHandlers> logger
  ) {
    _context = context;
    _userContext = userContext;
    _tenantContext = tenantContext;
    _logger = logger;
  }

  public async Task<CreateProjectResult> Handle(CreateProjectCommand request, CancellationToken cancellationToken) {
    try {
      _logger.LogInformation("Creating project: {Title} by user {UserId}", request.Title, _userContext.UserId);

      // Validate user permissions
      if (!_userContext.IsAuthenticated || _userContext.UserId == null) { return new CreateProjectResult { Success = false, ErrorMessage = "User must be authenticated" }; }

      // Create project entity
      var project = new Project {
        Id = Guid.NewGuid(),
        Title = request.Title,
        Description = request.Description,
        ShortDescription = request.ShortDescription,
        ImageUrl = request.ImageUrl,
        RepositoryUrl = request.RepositoryUrl,
        WebsiteUrl = request.WebsiteUrl,
        DownloadUrl = request.DownloadUrl,
        Type = (GameGuild.Common.ProjectType)request.Type,
        CategoryId = request.CategoryId,
        Visibility = request.Visibility,
        Status = request.Status,
        TenantId = request.TenantId ?? _tenantContext.TenantId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };

      // Generate slug from name
      project.Slug = GenerateSlug(request.Title);

      // Ensure slug is unique
      var existingSlugCount = await _context.Projects
                                            .Where(p => p.Slug.StartsWith(project.Slug) && p.DeletedAt == null)
                                            .CountAsync(cancellationToken);

      if (existingSlugCount > 0) { project.Slug = $"{project.Slug}-{existingSlugCount + 1}"; }

      _context.Projects.Add(project);

      // Add the creator as a collaborator with all permissions
      var creatorCollaborator = new ProjectCollaborator {
        Id = Guid.NewGuid(),
        ProjectId = project.Id,
        UserId = _userContext.UserId!.Value,
        Role = ProjectRoles.Owner,
        Permissions = FormatOwnerPermissions(),
        IsActive = true,
        JoinedAt = DateTime.UtcNow,
        Title = "Project Creator",
        Description = "Original creator of the project",
        Visibility = AccessLevel.Private,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };
      _context.Set<ProjectCollaborator>().Add(creatorCollaborator);

      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Project created successfully: {ProjectId}", project.Id);

      return new CreateProjectResult { Success = true, Project = project };
    }
    catch (Exception ex) {
      _logger.LogError(ex, "ErrorMessage creating project: {Title}", request.Title);

      return new CreateProjectResult { Success = false, ErrorMessage = "Failed to create project" };
    }
  }

  public async Task<UpdateProjectResult> Handle(UpdateProjectCommand request, CancellationToken cancellationToken) {
    try {
      _logger.LogInformation("Updating project: {ProjectId} by user {UserId}", request.ProjectId, _userContext.UserId);

      var project = await _context.Projects
                                  .Include(p => p.Collaborators)
                                  .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.DeletedAt == null, cancellationToken);

      if (project == null) { return new UpdateProjectResult { Success = false, ErrorMessage = "Project not found" }; }

      // Check authorization - user must have edit permissions
      var hasEditPermission = project.Collaborators.Any(c =>
                                                          c.UserId == _userContext.UserId &&
                                                          c.IsActive &&
                                                          c.Permissions.Contains(PermissionType.Edit.ToString())
      );

      if (!hasEditPermission) { return new UpdateProjectResult { Success = false, ErrorMessage = "Unauthorized to update this project" }; }

      // Update fields
      if (request.Title != null) project.Title = request.Title;
      if (request.Description != null) project.Description = request.Description;
      if (request.ShortDescription != null) project.ShortDescription = request.ShortDescription;
      if (request.ImageUrl != null) project.ImageUrl = request.ImageUrl;
      if (request.RepositoryUrl != null) project.RepositoryUrl = request.RepositoryUrl;
      if (request.WebsiteUrl != null) project.WebsiteUrl = request.WebsiteUrl;
      if (request.DownloadUrl != null) project.DownloadUrl = request.DownloadUrl;
      if (request.Type.HasValue) project.Type = (GameGuild.Common.ProjectType)request.Type.Value;
      if (request.CategoryId.HasValue) project.CategoryId = request.CategoryId;
      if (request.Visibility.HasValue) project.Visibility = request.Visibility.Value;
      if (request.Status.HasValue) project.Status = request.Status.Value;

      project.UpdatedAt = DateTime.UtcNow;

      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Project updated successfully: {ProjectId}", project.Id);

      return new UpdateProjectResult { Success = true, Project = project };
    }
    catch (Exception ex) {
      _logger.LogError(ex, "ErrorMessage updating project: {ProjectId}", request.ProjectId);

      return new UpdateProjectResult { Success = false, ErrorMessage = "Failed to update project" };
    }
  }

  public async Task<DeleteProjectResult> Handle(DeleteProjectCommand request, CancellationToken cancellationToken) {
    try {
      _logger.LogInformation("Deleting project: {ProjectId} by user {UserId}", request.ProjectId, _userContext.UserId);

      var project = await _context.Projects
                                  .Include(p => p.Collaborators)
                                  .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.DeletedAt == null, cancellationToken);

      if (project == null) { return new DeleteProjectResult { Success = false, ErrorMessage = "Project not found" }; }

      // Check authorization - user must have delete permissions
      var hasDeletePermission = project.Collaborators.Any(c =>
                                                            c.UserId == _userContext.UserId &&
                                                            c.IsActive &&
                                                            c.Permissions.Contains(PermissionType.Delete.ToString())
      );

      if (!hasDeletePermission) { return new DeleteProjectResult { Success = false, ErrorMessage = "Unauthorized to delete this project" }; }

      if (request.SoftDelete) {
        // Mark as deleted but preserve data
        // Use the DeletedAt property from the base Entity for soft delete
        project.DeletedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;
      }
      else { _context.Projects.Remove(project); }

      await _context.SaveChangesAsync(cancellationToken);

      _logger.LogInformation("Project deleted successfully: {ProjectId}", project.Id);

      return new DeleteProjectResult { Success = true };
    }
    catch (Exception ex) {
      _logger.LogError(ex, "ErrorMessage deleting project: {ProjectId}", request.ProjectId);

      return new DeleteProjectResult { Success = false, ErrorMessage = "Failed to delete project" };
    }
  }

  public async Task<PublishProjectResult> Handle(PublishProjectCommand request, CancellationToken cancellationToken) {
    try {
      var project = await _context.Projects
                                  .Include(p => p.Collaborators)
                                  .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.DeletedAt == null, cancellationToken);

      if (project == null) { return new PublishProjectResult { Success = false, ErrorMessage = "Project not found" }; }

      // Check authorization - user must have publish permissions
      var hasPublishPermission = project.Collaborators.Any(c =>
                                                             c.UserId == _userContext.UserId &&
                                                             c.IsActive &&
                                                             c.Permissions.Contains(PermissionType.Publish.ToString())
      );

      if (!hasPublishPermission) { return new PublishProjectResult { Success = false, ErrorMessage = "Unauthorized to publish this project" }; }

      project.Status = ContentStatus.Published;
      project.UpdatedAt = DateTime.UtcNow;

      await _context.SaveChangesAsync(cancellationToken);

      return new PublishProjectResult { Success = true, Project = project };
    }
    catch (Exception ex) {
      _logger.LogError(ex, "ErrorMessage publishing project: {ProjectId}", request.ProjectId);

      return new PublishProjectResult { Success = false, ErrorMessage = "Failed to publish project" };
    }
  }

  public async Task<UnpublishProjectResult> Handle(UnpublishProjectCommand request, CancellationToken cancellationToken) {
    try {
      var project = await _context.Projects
                                  .Include(p => p.Collaborators)
                                  .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.DeletedAt == null, cancellationToken);

      if (project == null) { return new UnpublishProjectResult { Success = false, ErrorMessage = "Project not found" }; }

      // Check authorization - user must have unpublish permissions
      var hasUnpublishPermission = project.Collaborators.Any(c =>
                                                               c.UserId == _userContext.UserId &&
                                                               c.IsActive &&
                                                               c.Permissions.Contains(PermissionType.Unpublish.ToString())
      );

      if (!hasUnpublishPermission) { return new UnpublishProjectResult { Success = false, ErrorMessage = "Unauthorized to unpublish this project" }; }

      project.Status = ContentStatus.Draft;
      project.UpdatedAt = DateTime.UtcNow;

      await _context.SaveChangesAsync(cancellationToken);

      return new UnpublishProjectResult { Success = true, Project = project };
    }
    catch (Exception ex) {
      _logger.LogError(ex, "ErrorMessage unpublishing project: {ProjectId}", request.ProjectId);

      return new UnpublishProjectResult { Success = false, ErrorMessage = "Failed to unpublish project" };
    }
  }

  public async Task<ArchiveProjectResult> Handle(ArchiveProjectCommand request, CancellationToken cancellationToken) {
    try {
      var project = await _context.Projects
                                  .Include(p => p.Collaborators)
                                  .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.DeletedAt == null, cancellationToken);

      if (project == null) { return new ArchiveProjectResult { Success = false, ErrorMessage = "Project not found" }; }

      // Check authorization - user must have archive permissions
      var hasArchivePermission = project.Collaborators.Any(c =>
                                                             c.UserId == _userContext.UserId &&
                                                             c.IsActive &&
                                                             c.Permissions.Contains(PermissionType.Archive.ToString())
      );

      if (!hasArchivePermission) { return new ArchiveProjectResult { Success = false, ErrorMessage = "Unauthorized to archive this project" }; }

      project.Status = ContentStatus.Archived;
      project.UpdatedAt = DateTime.UtcNow;

      await _context.SaveChangesAsync(cancellationToken);

      return new ArchiveProjectResult { Success = true, Project = project };
    }
    catch (Exception ex) {
      _logger.LogError(ex, "ErrorMessage archiving project: {ProjectId}", request.ProjectId);

      return new ArchiveProjectResult { Success = false, ErrorMessage = "Failed to archive project" };
    }
  }

  private static string GenerateSlug(string name) { return name.ToSlugCase(); }

  /// <summary>
  /// Format permissions for an owner collaborator
  /// </summary>
  private static string FormatOwnerPermissions() {
    var ownerPermissions = new[] {
      PermissionType.Read, PermissionType.Edit, PermissionType.Delete, PermissionType.Publish, PermissionType.Unpublish, PermissionType.Archive, PermissionType.Create, PermissionType.Approve, PermissionType.Monetize
    };

    return string.Join(",", ownerPermissions.Select(p => p.ToString()));
  }

  /// <summary>
  /// Format permissions for an editor collaborator
  /// </summary>
  private static string FormatEditorPermissions() {
    var editorPermissions = new[] { PermissionType.Read, PermissionType.Edit, PermissionType.Comment, PermissionType.Submit };

    return string.Join(",", editorPermissions.Select(p => p.ToString()));
  }
}
