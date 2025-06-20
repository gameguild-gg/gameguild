using Microsoft.AspNetCore.Mvc;
using GameGuild.Modules.Project.Services;
using GameGuild.Common.Entities;
using GameGuild.Common.Attributes;
using GameGuild.Modules.Auth.Attributes;

namespace GameGuild.Modules.Project.Controllers;

[ApiController]
[Route("[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    // GET: projects
    [HttpGet]
    [Public]
    public async Task<ActionResult<IEnumerable<Models.Project>>> GetProjects()
    {
        var projects = await _projectService.GetAllProjectsAsync();
        return Ok(projects);
    }

    // GET: projects/{id}
    [HttpGet("{id}")]
    [RequireResourcePermission<Models.Project>(PermissionType.Read)]
    public async Task<ActionResult<Models.Project>> GetProject(Guid id)
    {
        var project = await _projectService.GetProjectByIdAsync(id);
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
        var project = await _projectService.GetProjectBySlugAsync(slug);
        if (project == null)
        {
            return NotFound();
        }
        return Ok(project);
    }

    // GET: projects/category/{categoryId}
    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<IEnumerable<Models.Project>>> GetProjectsByCategory(Guid categoryId)
    {
        var projects = await _projectService.GetProjectsByCategoryAsync(categoryId);
        return Ok(projects);
    }

    // GET: projects/creator/{creatorId}
    [HttpGet("creator/{creatorId}")]
    public async Task<ActionResult<IEnumerable<Models.Project>>> GetProjectsByCreator(Guid creatorId)
    {
        var projects = await _projectService.GetProjectsByCreatorAsync(creatorId);
        return Ok(projects);
    }

    // GET: projects/status/{status}
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<Models.Project>>> GetProjectsByStatus(ContentStatus status)
    {
        var projects = await _projectService.GetProjectsByStatusAsync(status);
        return Ok(projects);
    }

    // POST: projects
    [HttpPost]
    [Public]
    public async Task<ActionResult<Models.Project>> CreateProject([FromBody] Models.Project project)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var createdProject = await _projectService.CreateProjectAsync(project);
        return CreatedAtAction(nameof(GetProject), new { id = createdProject.Id }, createdProject);
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
            var updatedProject = await _projectService.UpdateProjectAsync(project);
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
        var success = await _projectService.DeleteProjectAsync(id);
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
        var success = await _projectService.RestoreProjectAsync(id);
        if (!success)
        {
            return NotFound();
        }
        return NoContent();
    }

    // GET: projects/deleted
    [HttpGet("deleted")]
    public async Task<ActionResult<IEnumerable<Models.Project>>> GetDeletedProjects()
    {
        var projects = await _projectService.GetDeletedProjectsAsync();
        return Ok(projects);
    }

    // GET: projects/public (public access for web app integration)
    [HttpGet("public")]
    public async Task<ActionResult<IEnumerable<Models.Project>>> GetPublicProjects()
    {
        // Only return published projects for public access
        var projects = await _projectService.GetProjectsByStatusAsync(ContentStatus.Published);
        return Ok(projects);
    }
}
