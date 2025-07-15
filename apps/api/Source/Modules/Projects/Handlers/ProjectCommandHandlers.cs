using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Contents;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Projects;

/// <summary>
/// Command handlers for project operations
/// </summary>
public class ProjectCommandHandlers : 
    IRequestHandler<CreateProjectCommand, CreateProjectResult>,
    IRequestHandler<UpdateProjectCommand, UpdateProjectResult>,
    IRequestHandler<DeleteProjectCommand, DeleteProjectResult>,
    IRequestHandler<PublishProjectCommand, PublishProjectResult>,
    IRequestHandler<UnpublishProjectCommand, UnpublishProjectResult>,
    IRequestHandler<ArchiveProjectCommand, ArchiveProjectResult>
{
    private readonly ApplicationDbContext _context;
    private readonly GameGuild.Common.Interfaces.IUserContext _userContext;
    private readonly GameGuild.Common.Interfaces.ITenantContext _tenantContext;
    private readonly ILogger<ProjectCommandHandlers> _logger;

    public ProjectCommandHandlers(
        ApplicationDbContext context,
        GameGuild.Common.Interfaces.IUserContext userContext,
        GameGuild.Common.Interfaces.ITenantContext tenantContext,
        ILogger<ProjectCommandHandlers> logger)
    {
        _context = context;
        _userContext = userContext;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<CreateProjectResult> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating project: {Title} by user {UserId}", request.Title, _userContext.UserId);

            // Validate user permissions
            if (!_userContext.IsAuthenticated || _userContext.UserId == null)
            {
                return new CreateProjectResult { Success = false, ErrorMessage = "User must be authenticated" };
            }

            // Check if user can create projects
            if (!_userContext.IsInRole("Admin") && !_userContext.IsInRole("Developer") && !_userContext.IsInRole("Creator"))
            {
                return new CreateProjectResult { Success = false, ErrorMessage = "User does not have permission to create projects" };
            }

            // Create project entity
            var project = new Project
            {
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
                .Where(p => p.Slug.StartsWith(project.Slug) && !p.IsDeleted)
                .CountAsync(cancellationToken);

            if (existingSlugCount > 0)
            {
                project.Slug = $"{project.Slug}-{existingSlugCount + 1}";
            }

            _context.Projects.Add(project);
            
            // Add the creator as a collaborator with Owner role
            var creatorCollaborator = new ProjectCollaborator
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = _userContext.UserId!.Value,
                Role = "Owner",
                Permissions = "All",
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

            return new CreateProjectResult
            {
                Success = true,
                Project = project
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project: {Title}", request.Title);
            return new CreateProjectResult
            {
                Success = false,
                ErrorMessage = "Failed to create project"
            };
        }
    }

    public async Task<UpdateProjectResult> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating project: {ProjectId} by user {UserId}", request.ProjectId, _userContext.UserId);

            var project = await _context.Projects
                .Include(p => p.Collaborators)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId && !p.IsDeleted, cancellationToken);

            if (project == null)
            {
                return new UpdateProjectResult { Success = false, ErrorMessage = "Project not found" };
            }

            // Check authorization - user must be admin or owner/collaborator with edit permissions
            var isOwnerOrCollaborator = project.Collaborators.Any(c => 
                c.UserId == _userContext.UserId && 
                c.IsActive && 
                (c.Role == "Owner" || c.Permissions.Contains("Edit")));
                
            if (!_userContext.IsInRole("Admin") && !isOwnerOrCollaborator)
            {
                return new UpdateProjectResult { Success = false, ErrorMessage = "Unauthorized to update this project" };
            }

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

            return new UpdateProjectResult
            {
                Success = true,
                Project = project
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project: {ProjectId}", request.ProjectId);
            return new UpdateProjectResult
            {
                Success = false,
                ErrorMessage = "Failed to update project"
            };
        }
    }

    public async Task<DeleteProjectResult> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting project: {ProjectId} by user {UserId}", request.ProjectId, _userContext.UserId);

            var project = await _context.Projects
                .Include(p => p.Collaborators)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId && !p.IsDeleted, cancellationToken);

            if (project == null)
            {
                return new DeleteProjectResult { Success = false, ErrorMessage = "Project not found" };
            }

            // Check authorization - user must be admin or owner
            var isOwner = project.Collaborators.Any(c => 
                c.UserId == _userContext.UserId && 
                c.IsActive && 
                c.Role == "Owner");
                
            if (!_userContext.IsInRole("Admin") && !isOwner)
            {
                return new DeleteProjectResult { Success = false, ErrorMessage = "Unauthorized to delete this project" };
            }

            if (request.SoftDelete)
            {
                // Mark as deleted but preserve data
                // Since IsDeleted is read-only in the base Entity, we'll need a different approach
                // For now, we'll remove it from the context or use a status change
                project.Status = ContentStatus.Archived; // Use archived status instead
                project.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.Projects.Remove(project);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Project deleted successfully: {ProjectId}", project.Id);

            return new DeleteProjectResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project: {ProjectId}", request.ProjectId);
            return new DeleteProjectResult
            {
                Success = false,
                ErrorMessage = "Failed to delete project"
            };
        }
    }

    public async Task<PublishProjectResult> Handle(PublishProjectCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId && !p.IsDeleted, cancellationToken);

            if (project == null)
            {
                return new PublishProjectResult { Success = false, ErrorMessage = "Project not found" };
            }

            project.Status = ContentStatus.Published;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return new PublishProjectResult { Success = true, Project = project };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing project: {ProjectId}", request.ProjectId);
            return new PublishProjectResult { Success = false, ErrorMessage = "Failed to publish project" };
        }
    }

    public async Task<UnpublishProjectResult> Handle(UnpublishProjectCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId && !p.IsDeleted, cancellationToken);

            if (project == null)
            {
                return new UnpublishProjectResult { Success = false, ErrorMessage = "Project not found" };
            }

            project.Status = ContentStatus.Draft;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return new UnpublishProjectResult { Success = true, Project = project };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpublishing project: {ProjectId}", request.ProjectId);
            return new UnpublishProjectResult { Success = false, ErrorMessage = "Failed to unpublish project" };
        }
    }

    public async Task<ArchiveProjectResult> Handle(ArchiveProjectCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId && !p.IsDeleted, cancellationToken);

            if (project == null)
            {
                return new ArchiveProjectResult { Success = false, ErrorMessage = "Project not found" };
            }

            project.Status = ContentStatus.Archived;
            project.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return new ArchiveProjectResult { Success = true, Project = project };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving project: {ProjectId}", request.ProjectId);
            return new ArchiveProjectResult { Success = false, ErrorMessage = "Failed to archive project" };
        }
    }

    private static string GenerateSlug(string name)
    {
        return name.ToSlugCase();
    }
}
