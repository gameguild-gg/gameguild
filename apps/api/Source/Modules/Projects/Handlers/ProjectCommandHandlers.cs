using GameGuild.Common.Interfaces;
using GameGuild.Database;
using GameGuild.Modules.Projects.Commands;
using GameGuild.Modules.Projects.Models;
using GameGuild.Modules.Contents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Projects.Handlers;

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
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ProjectCommandHandlers> _logger;

    public ProjectCommandHandlers(
        ApplicationDbContext context,
        IUserContext userContext,
        ITenantContext tenantContext,
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
            _logger.LogInformation("Creating project: {Name} by user {UserId}", request.Name, _userContext.UserId);

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
                Name = request.Name,
                Description = request.Description,
                ShortDescription = request.ShortDescription,
                ImageUrl = request.ImageUrl,
                RepositoryUrl = request.RepositoryUrl,
                DemoUrl = request.DemoUrl,
                DocumentationUrl = request.DocumentationUrl,
                Type = request.Type,
                CreatorId = request.CreatorId,
                CategoryId = request.CategoryId,
                Visibility = request.Visibility,
                Status = request.Status,
                TenantId = request.TenantId ?? _tenantContext.TenantId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Generate slug from name
            project.Slug = GenerateSlug(request.Name);

            // Ensure slug is unique
            var existingSlugCount = await _context.Projects
                .Where(p => p.Slug.StartsWith(project.Slug) && !p.IsDeleted)
                .CountAsync(cancellationToken);

            if (existingSlugCount > 0)
            {
                project.Slug = $"{project.Slug}-{existingSlugCount + 1}";
            }

            _context.Projects.Add(project);
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
            _logger.LogError(ex, "Error creating project: {Name}", request.Name);
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
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId && !p.IsDeleted, cancellationToken);

            if (project == null)
            {
                return new UpdateProjectResult { Success = false, ErrorMessage = "Project not found" };
            }

            // Check authorization
            if (!_userContext.IsInRole("Admin") && project.CreatorId != _userContext.UserId)
            {
                return new UpdateProjectResult { Success = false, ErrorMessage = "Unauthorized to update this project" };
            }

            // Update fields
            if (request.Name != null) project.Name = request.Name;
            if (request.Description != null) project.Description = request.Description;
            if (request.ShortDescription != null) project.ShortDescription = request.ShortDescription;
            if (request.ImageUrl != null) project.ImageUrl = request.ImageUrl;
            if (request.RepositoryUrl != null) project.RepositoryUrl = request.RepositoryUrl;
            if (request.DemoUrl != null) project.DemoUrl = request.DemoUrl;
            if (request.DocumentationUrl != null) project.DocumentationUrl = request.DocumentationUrl;
            if (request.Type.HasValue) project.Type = request.Type.Value;
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
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId && !p.IsDeleted, cancellationToken);

            if (project == null)
            {
                return new DeleteProjectResult { Success = false, ErrorMessage = "Project not found" };
            }

            // Check authorization
            if (!_userContext.IsInRole("Admin") && project.CreatorId != _userContext.UserId)
            {
                return new DeleteProjectResult { Success = false, ErrorMessage = "Unauthorized to delete this project" };
            }

            if (request.SoftDelete)
            {
                project.IsDeleted = true;
                project.DeletedAt = DateTime.UtcNow;
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
        return name.ToLowerInvariant()
                   .Replace(" ", "-")
                   .Replace("_", "-")
                   .Trim('-');
    }
}
