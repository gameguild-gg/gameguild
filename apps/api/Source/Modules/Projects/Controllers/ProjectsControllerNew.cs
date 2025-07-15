using GameGuild.Common.Interfaces;
using GameGuild.Modules.Projects.Commands;
using GameGuild.Modules.Projects.Queries;
using GameGuild.Modules.Projects.Models;
using GameGuild.Modules.Contents;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace GameGuild.Modules.Projects.Controllers;

/// <summary>
/// REST API controller for managing projects using CQRS pattern
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        IMediator mediator,
        IUserContext userContext,
        ITenantContext tenantContext,
        ILogger<ProjectsController> logger)
    {
        _mediator = mediator;
        _userContext = userContext;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all projects with filtering and pagination
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjects(
        [FromQuery] ProjectType? type = null,
        [FromQuery] ContentStatus? status = null,
        [FromQuery] AccessLevel? visibility = null,
        [FromQuery] Guid? creatorId = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? sortBy = "CreatedAt",
        [FromQuery] string? sortDirection = "DESC")
    {
        var query = new GetAllProjectsQuery
        {
            Type = type,
            Status = status,
            Visibility = visibility,
            CreatorId = creatorId,
            CategoryId = categoryId,
            SearchTerm = searchTerm,
            Skip = skip,
            Take = Math.Min(take, 100), // Limit max items
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var projects = await _mediator.Send(query);
        return Ok(projects);
    }

    /// <summary>
    /// Get project by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<Project>> GetProject(
        Guid id,
        [FromQuery] bool includeTeam = true,
        [FromQuery] bool includeReleases = true,
        [FromQuery] bool includeCollaborators = true,
        [FromQuery] bool includeStatistics = false)
    {
        var query = new GetProjectByIdQuery
        {
            ProjectId = id,
            IncludeTeam = includeTeam,
            IncludeReleases = includeReleases,
            IncludeCollaborators = includeCollaborators,
            IncludeStatistics = includeStatistics
        };

        var project = await _mediator.Send(query);
        
        if (project == null)
        {
            return NotFound();
        }

        return Ok(project);
    }

    /// <summary>
    /// Get project by slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<Project>> GetProjectBySlug(
        string slug,
        [FromQuery] bool includeTeam = true,
        [FromQuery] bool includeReleases = true,
        [FromQuery] bool includeCollaborators = true)
    {
        var query = new GetProjectBySlugQuery
        {
            Slug = slug,
            IncludeTeam = includeTeam,
            IncludeReleases = includeReleases,
            IncludeCollaborators = includeCollaborators
        };

        var project = await _mediator.Send(query);
        
        if (project == null)
        {
            return NotFound();
        }

        return Ok(project);
    }

    /// <summary>
    /// Create a new project
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CreateProjectResult>> CreateProject([FromBody] CreateProjectRequest request)
    {
        var command = new CreateProjectCommand
        {
            Name = request.Name,
            Description = request.Description,
            ShortDescription = request.ShortDescription,
            ImageUrl = request.ImageUrl,
            RepositoryUrl = request.RepositoryUrl,
            DemoUrl = request.DemoUrl,
            DocumentationUrl = request.DocumentationUrl,
            Type = request.Type,
            CreatorId = _userContext.UserId ?? Guid.Empty,
            CategoryId = request.CategoryId,
            Visibility = request.Visibility,
            Status = request.Status,
            Tags = request.Tags,
            TenantId = _tenantContext.TenantId
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetProject), new { id = result.Project!.Id }, result);
    }

    /// <summary>
    /// Update an existing project
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UpdateProjectResult>> UpdateProject(Guid id, [FromBody] UpdateProjectRequest request)
    {
        var command = new UpdateProjectCommand
        {
            ProjectId = id,
            Name = request.Name,
            Description = request.Description,
            ShortDescription = request.ShortDescription,
            ImageUrl = request.ImageUrl,
            RepositoryUrl = request.RepositoryUrl,
            DemoUrl = request.DemoUrl,
            DocumentationUrl = request.DocumentationUrl,
            Type = request.Type,
            CategoryId = request.CategoryId,
            Visibility = request.Visibility,
            Status = request.Status,
            Tags = request.Tags,
            UpdatedBy = _userContext.UserId ?? Guid.Empty
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a project
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<DeleteProjectResult>> DeleteProject(
        Guid id, 
        [FromQuery] bool softDelete = true, 
        [FromQuery] string? reason = null)
    {
        var command = new DeleteProjectCommand
        {
            ProjectId = id,
            DeletedBy = _userContext.UserId ?? Guid.Empty,
            SoftDelete = softDelete,
            Reason = reason
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Publish a project
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<PublishProjectResult>> PublishProject(Guid id)
    {
        var command = new PublishProjectCommand
        {
            ProjectId = id,
            PublishedBy = _userContext.UserId ?? Guid.Empty
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Unpublish a project
    /// </summary>
    [HttpPost("{id:guid}/unpublish")]
    public async Task<ActionResult<UnpublishProjectResult>> UnpublishProject(Guid id)
    {
        var command = new UnpublishProjectCommand
        {
            ProjectId = id,
            UnpublishedBy = _userContext.UserId ?? Guid.Empty
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Archive a project
    /// </summary>
    [HttpPost("{id:guid}/archive")]
    public async Task<ActionResult<ArchiveProjectResult>> ArchiveProject(Guid id)
    {
        var command = new ArchiveProjectCommand
        {
            ProjectId = id,
            ArchivedBy = _userContext.UserId ?? Guid.Empty
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Search projects
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Project>>> SearchProjects(
        [FromQuery] string searchTerm,
        [FromQuery] ProjectType? type = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] ContentStatus? status = null,
        [FromQuery] AccessLevel? visibility = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? sortBy = "Relevance",
        [FromQuery] string? sortDirection = "DESC")
    {
        var query = new SearchProjectsQuery
        {
            SearchTerm = searchTerm,
            Type = type,
            CategoryId = categoryId,
            Status = status,
            Visibility = visibility,
            Skip = skip,
            Take = Math.Min(take, 100),
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var projects = await _mediator.Send(query);
        return Ok(projects);
    }

    /// <summary>
    /// Get popular projects
    /// </summary>
    [HttpGet("popular")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Project>>> GetPopularProjects(
        [FromQuery] ProjectType? type = null,
        [FromQuery] int take = 10)
    {
        var query = new GetPopularProjectsQuery
        {
            Type = type,
            Take = Math.Min(take, 50)
        };

        var projects = await _mediator.Send(query);
        return Ok(projects);
    }

    /// <summary>
    /// Get recent projects
    /// </summary>
    [HttpGet("recent")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Project>>> GetRecentProjects(
        [FromQuery] ProjectType? type = null,
        [FromQuery] int take = 10)
    {
        var query = new GetRecentProjectsQuery
        {
            Type = type,
            Take = Math.Min(take, 50)
        };

        var projects = await _mediator.Send(query);
        return Ok(projects);
    }

    /// <summary>
    /// Get featured projects
    /// </summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Project>>> GetFeaturedProjects(
        [FromQuery] ProjectType? type = null,
        [FromQuery] int take = 10)
    {
        var query = new GetFeaturedProjectsQuery
        {
            Type = type,
            Take = Math.Min(take, 50)
        };

        var projects = await _mediator.Send(query);
        return Ok(projects);
    }

    /// <summary>
    /// Get project statistics
    /// </summary>
    [HttpGet("{id:guid}/statistics")]
    [AllowAnonymous]
    public async Task<ActionResult<ProjectStatistics>> GetProjectStatistics(
        Guid id,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = new GetProjectStatisticsQuery
        {
            ProjectId = id,
            FromDate = fromDate,
            ToDate = toDate
        };

        var statistics = await _mediator.Send(query);
        return Ok(statistics);
    }

    /// <summary>
    /// Get projects by category
    /// </summary>
    [HttpGet("category/{categoryId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjectsByCategory(
        Guid categoryId,
        [FromQuery] ContentStatus? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = new GetProjectsByCategoryQuery
        {
            CategoryId = categoryId,
            Status = status,
            Skip = skip,
            Take = Math.Min(take, 100)
        };

        var projects = await _mediator.Send(query);
        return Ok(projects);
    }

    /// <summary>
    /// Get projects by creator
    /// </summary>
    [HttpGet("creator/{creatorId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjectsByCreator(
        Guid creatorId,
        [FromQuery] ContentStatus? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = new GetProjectsByCreatorQuery
        {
            CreatorId = creatorId,
            Status = status,
            Skip = skip,
            Take = Math.Min(take, 100)
        };

        var projects = await _mediator.Send(query);
        return Ok(projects);
    }
}

/// <summary>
/// Request DTOs for REST API
/// </summary>
public record CreateProjectRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ShortDescription { get; init; }
    public string? ImageUrl { get; init; }
    public string? RepositoryUrl { get; init; }
    public string? DemoUrl { get; init; }
    public string? DocumentationUrl { get; init; }
    public ProjectType Type { get; init; } = ProjectType.Game;
    public Guid? CategoryId { get; init; }
    public AccessLevel Visibility { get; init; } = AccessLevel.Public;
    public ContentStatus Status { get; init; } = ContentStatus.Draft;
    public List<string>? Tags { get; init; }
}

public record UpdateProjectRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? ShortDescription { get; init; }
    public string? ImageUrl { get; init; }
    public string? RepositoryUrl { get; init; }
    public string? DemoUrl { get; init; }
    public string? DocumentationUrl { get; init; }
    public ProjectType? Type { get; init; }
    public Guid? CategoryId { get; init; }
    public AccessLevel? Visibility { get; init; }
    public ContentStatus? Status { get; init; }
    public List<string>? Tags { get; init; }
}
