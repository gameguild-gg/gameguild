using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameGuild.Modules.Project.Services;
using GameGuild.Common.Entities;
using GameGuild.Common.Attributes;
using GameGuild.Modules.Auth.Attributes;
using System.Security.Claims;

namespace GameGuild.Modules.Project.Controllers;

[ApiController]
[Route("[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }    // GET: projects
    [HttpGet]
    [RequireResourcePermission<Models.Project>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<Models.Project>>> GetProjects()
    {
        // Get the current authenticated user's ID
        string? userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
        {
            return Unauthorized("User ID not found in token");
        }

        // Return only projects created by the authenticated user
        var projects = await _projectService.GetProjectsByCreatorAsync(userId);

        return Ok(projects);
    }

    // GET: projects/{id}
    [HttpGet("{id}")]
    [RequireResourcePermission<Models.Project>(PermissionType.Read)]
    public async Task<ActionResult<Models.Project>> GetProject(Guid id)
    {
        Models.Project? project = await _projectService.GetProjectByIdAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        return Ok(project);
    }

    // GET: projects/slug/{slug}
    [HttpGet("slug/{slug}")]
    [Public]
    public async Task<ActionResult<Models.Project>> GetProjectBySlug(string slug)
    {
        Models.Project? project = await _projectService.GetProjectBySlugAsync(slug);
        if (project == null)
        {
            return NotFound();
        }

        return Ok(project);
    }

    // GET: projects/category/{categoryId}
    [HttpGet("category/{categoryId}")]
    [RequireResourcePermission<Models.Project>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<Models.Project>>> GetProjectsByCategory(Guid categoryId)
    {
        var projects = await _projectService.GetProjectsByCategoryAsync(categoryId);

        return Ok(projects);
    }

    // GET: projects/creator/{creatorId}
    [HttpGet("creator/{creatorId}")]
    [RequireResourcePermission<Models.Project>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<Models.Project>>> GetProjectsByCreator(Guid creatorId)
    {
        var projects = await _projectService.GetProjectsByCreatorAsync(creatorId);

        return Ok(projects);
    }

    // GET: projects/status/{status}
    [HttpGet("status/{status}")]
    [RequireResourcePermission<Models.Project>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<Models.Project>>> GetProjectsByStatus(ContentStatus status)
    {
        var projects = await _projectService.GetProjectsByStatusAsync(status);

        return Ok(projects);
    }    // POST: projects
    [HttpPost]
    [RequireResourcePermission<Models.Project>(PermissionType.Create)]
    public async Task<ActionResult<Models.Project>> CreateProject([FromBody] Models.Project project)
    {
        // Get the current authenticated user's ID
        string? userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
        {
            return Unauthorized("User ID not found in token");
        }

        // Set the creator ID from the authenticated user
        project.CreatedById = userId;

        // Auto-generate slug if not provided
        if (string.IsNullOrEmpty(project.Slug))
        {
            project.Slug = Models.Project.GenerateSlug(project.Title);
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        Models.Project createdProject = await _projectService.CreateProjectAsync(project);

        return CreatedAtAction(
            nameof(GetProject),
            new
            {
                id = createdProject.Id
            },
            createdProject
        );
    }

    // PUT: projects/{id}
    [HttpPut("{id}")]
    [RequireResourcePermission<Models.Project>(PermissionType.Edit)]
    public async Task<ActionResult<Models.Project>> UpdateProject(Guid id, [FromBody] Models.Project project)
    {
        if (id != project.Id)
        {
            return BadRequest("Project ID mismatch");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            Models.Project updatedProject = await _projectService.UpdateProjectAsync(project);

            return Ok(updatedProject);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    // DELETE: projects/{id}
    [HttpDelete("{id}")]
    [RequireResourcePermission<Models.Project>(PermissionType.Delete)]
    public async Task<ActionResult> DeleteProject(Guid id)
    {
        bool success = await _projectService.DeleteProjectAsync(id);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    // POST: projects/{id}/restore
    [HttpPost("{id}/restore")]
    [RequireResourcePermission<Models.Project>(PermissionType.Restore)]
    public async Task<ActionResult> RestoreProject(Guid id)
    {
        bool success = await _projectService.RestoreProjectAsync(id);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    // GET: projects/deleted
    [HttpGet("deleted")]
    [RequireResourcePermission<Models.Project>(PermissionType.Read)]
    public async Task<ActionResult<IEnumerable<Models.Project>>> GetDeletedProjects()
    {
        var projects = await _projectService.GetDeletedProjectsAsync();

        return Ok(projects);
    } // GET: projects/public (public access for web app integration)

    [HttpGet("public")]
    [Public]
    public async Task<ActionResult<IEnumerable<Models.Project>>> GetPublicProjects()
    {
        // Only return published AND public visibility projects for public access
        var projects = await _projectService.GetPublicProjectsAsync(0, 50);

        return Ok(projects);
    }
}
