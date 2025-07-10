using System.Security.Claims;
using GameGuild.Common;
using GameGuild.Modules.Auth;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Permissions.Models;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Modules.Projects;

[ApiController]
[Route("[controller]")]
public class ProjectsController(IProjectService projectService) : ControllerBase {
  [HttpGet]
  [RequireResourcePermission<Project>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<Project>>> GetProjects() {
    // Get the current authenticated user's ID
    var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized("User ID not found in token");

    // Return only projects created by the authenticated user
    var projects = await projectService.GetProjectsByCreatorAsync(userId);

    return Ok(projects);
  }

  // GET: projects/{id}
  [HttpGet("{id:guid}")]
  [RequireResourcePermission<Project>(PermissionType.Read)]
  public async Task<ActionResult<Project>> GetProject(Guid id) {
    var project = await projectService.GetProjectByIdAsync(id);

    if (project == null) return NotFound();

    return Ok(project);
  }

  // GET: projects/slug/{slug}
  [HttpGet("slug/{slug}")]
  [Public]
  public async Task<ActionResult<Project>> GetProjectBySlug(string slug) {
    var project = await projectService.GetProjectBySlugAsync(slug);

    if (project == null) return NotFound();

    return Ok(project);
  }

  // GET: projects/category/{categoryId}
  [HttpGet("category/{categoryId:guid}")]
  [RequireResourcePermission<Project>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<Project>>> GetProjectsByCategory(Guid categoryId) {
    var projects = await projectService.GetProjectsByCategoryAsync(categoryId);

    return Ok(projects);
  }

  // GET: projects/creator/{creatorId}
  [HttpGet("creator/{creatorId:guid}")]
  [RequireResourcePermission<Project>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<Project>>> GetProjectsByCreator(Guid creatorId) {
    var projects = await projectService.GetProjectsByCreatorAsync(creatorId);

    return Ok(projects);
  }

  // GET: projects/status/{status}
  [HttpGet("status/{status}")]
  [RequireResourcePermission<Project>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<Project>>> GetProjectsByStatus(ContentStatus status) {
    var projects = await projectService.GetProjectsByStatusAsync(status);

    return Ok(projects);
  } // POST: projects

  [HttpPost]
  [RequireResourcePermission<Project>(PermissionType.Create)]
  public async Task<ActionResult<Project>> CreateProject([FromBody] Project project) {
    // Get the current authenticated user's ID
    var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) return Unauthorized("User ID not found in token");

    // Set the creator ID from the authenticated user
    project.CreatedById = userId;

    // Auto-generate slug if not provided
    if (string.IsNullOrEmpty(project.Slug)) project.Slug = Project.GenerateSlug(project.Title);

    if (!ModelState.IsValid) return BadRequest(ModelState);

    var createdProject = await projectService.CreateProjectAsync(project);

    return CreatedAtAction(nameof(GetProject), new { id = createdProject.Id }, createdProject);
  }

  // PUT: projects/{id}
  [HttpPut("{id}")]
  [RequireResourcePermission<Project>(PermissionType.Edit)]
  public async Task<ActionResult<Project>> UpdateProject(Guid id, [FromBody] Project project) {
    if (id != project.Id) return BadRequest("Project ID mismatch");

    if (!ModelState.IsValid) return BadRequest(ModelState);

    try {
      var updatedProject = await projectService.UpdateProjectAsync(project);

      return Ok(updatedProject);
    }
    catch (InvalidOperationException ex) { return NotFound(ex.Message); }
  }

  // DELETE: projects/{id}
  [HttpDelete("{id}")]
  [RequireResourcePermission<Project>(PermissionType.Delete)]
  public async Task<ActionResult> DeleteProject(Guid id) {
    var success = await projectService.DeleteProjectAsync(id);

    if (!success) return NotFound();

    return NoContent();
  }

  // POST: projects/{id}/restore
  [HttpPost("{id}/restore")]
  [RequireResourcePermission<Project>(PermissionType.Restore)]
  public async Task<ActionResult> RestoreProject(Guid id) {
    var success = await projectService.RestoreProjectAsync(id);

    if (!success) return NotFound();

    return NoContent();
  }

  // GET: projects/deleted
  [HttpGet("deleted")]
  [RequireResourcePermission<Project>(PermissionType.Read)]
  public async Task<ActionResult<IEnumerable<Project>>> GetDeletedProjects() {
    var projects = await projectService.GetDeletedProjectsAsync();

    return Ok(projects);
  }

  // GET: projects/public (public access for web app integration)
  [HttpGet("public")]
  [Public]
  public async Task<ActionResult<IEnumerable<Project>>> GetPublicProjects() {
    // Only return published AND public visibility projects for public access
    var projects = await projectService.GetPublicProjectsAsync();

    return Ok(projects);
  }
}
