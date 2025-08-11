using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Contents; // For AccessLevel
using GameGuild.Modules.Permissions; // For PermissionType
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Projects {

  /// <summary>
  /// REST API controller for managing project versions
  /// </summary>
  [ApiController]
  [Route("api/projects/{projectId:guid}/versions")]
  [Authorize]
  public class ProjectVersionsController : ControllerBase {
    private readonly ApplicationDbContext _context;
    private readonly IUserContext _userContext;
    private readonly ILogger<ProjectVersionsController> _logger;

    public ProjectVersionsController(ApplicationDbContext context, IUserContext userContext, ILogger<ProjectVersionsController> logger) {
      _context = context;
      _userContext = userContext;
      _logger = logger;
    }

    /// <summary>
    /// Get all versions for a project
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectVersion>>> GetVersions(Guid projectId) {
      var project = await _context.Projects.Include(p => p.Collaborators)
                                           .FirstOrDefaultAsync(p => p.Id == projectId && p.DeletedAt == null);
      if (project == null) { return NotFound(); }
      if (!UserCanRead(project)) { return Forbid(); }

      var versions = await _context.Set<ProjectVersion>()
                                   .Where(v => v.ProjectId == projectId && v.DeletedAt == null)
                                   .OrderByDescending(v => v.CreatedAt)
                                   .ToListAsync();
      return Ok(versions);
    }

    /// <summary>
    /// Get a specific version
    /// </summary>
    [HttpGet("{versionId:guid}")]
    public async Task<ActionResult<ProjectVersion>> GetVersion(Guid projectId, Guid versionId) {
      var version = await _context.Set<ProjectVersion>()
                                  .Include(v => v.Project)
                                  .ThenInclude(p => p.Collaborators)
                                  .FirstOrDefaultAsync(v => v.Id == versionId && v.ProjectId == projectId && v.DeletedAt == null);
      if (version == null) { return NotFound(); }
      if (!UserCanRead(version.Project)) { return Forbid(); }
      return Ok(version);
    }

    /// <summary>
    /// Create a new version
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProjectVersion>> CreateVersion(Guid projectId, [FromBody] CreateProjectVersionRequest request) {
      if (!_userContext.IsAuthenticated || _userContext.UserId == null) { return Unauthorized(); }
      var project = await _context.Projects.Include(p => p.Collaborators)
                                           .FirstOrDefaultAsync(p => p.Id == projectId && p.DeletedAt == null);
      if (project == null) { return NotFound(); }
      if (!UserCanEdit(project)) { return Forbid(); }

      var version = new ProjectVersion {
        Id = Guid.NewGuid(),
        ProjectId = projectId,
        VersionNumber = request.VersionNumber.Trim(),
        ReleaseNotes = request.ReleaseNotes,
        Status = string.IsNullOrWhiteSpace(request.Status) ? "draft" : request.Status!,
        CreatedById = _userContext.UserId.Value,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
      };

      _context.Set<ProjectVersion>().Add(version);
      await _context.SaveChangesAsync();
      return CreatedAtAction(nameof(GetVersion), new { projectId, versionId = version.Id }, version);
    }

    /// <summary>
    /// Update an existing version
    /// </summary>
    [HttpPut("{versionId:guid}")]
    public async Task<ActionResult<ProjectVersion>> UpdateVersion(Guid projectId, Guid versionId, [FromBody] UpdateProjectVersionRequest request) {
      if (!_userContext.IsAuthenticated || _userContext.UserId == null) { return Unauthorized(); }
      var version = await _context.Set<ProjectVersion>()
                                  .Include(v => v.Project)
                                  .ThenInclude(p => p.Collaborators)
                                  .FirstOrDefaultAsync(v => v.Id == versionId && v.ProjectId == projectId && v.DeletedAt == null);
      if (version == null) { return NotFound(); }
      if (!UserCanEdit(version.Project)) { return Forbid(); }

      if (!string.IsNullOrWhiteSpace(request.VersionNumber)) { version.VersionNumber = request.VersionNumber.Trim(); }
      if (request.ReleaseNotes != null) { version.ReleaseNotes = request.ReleaseNotes; }
      if (!string.IsNullOrWhiteSpace(request.Status)) { version.Status = request.Status!; }
      version.UpdatedAt = DateTime.UtcNow;
      await _context.SaveChangesAsync();
      return Ok(version);
    }

    /// <summary>
    /// Delete a version (soft delete)
    /// </summary>
    [HttpDelete("{versionId:guid}")]
    public async Task<ActionResult> DeleteVersion(Guid projectId, Guid versionId, [FromQuery] bool softDelete = true) {
      if (!_userContext.IsAuthenticated || _userContext.UserId == null) { return Unauthorized(); }
      var version = await _context.Set<ProjectVersion>()
                                  .Include(v => v.Project)
                                  .ThenInclude(p => p.Collaborators)
                                  .FirstOrDefaultAsync(v => v.Id == versionId && v.ProjectId == projectId && v.DeletedAt == null);
      if (version == null) { return NotFound(); }
      if (!UserCanEdit(version.Project)) { return Forbid(); }
      if (softDelete) {
        version.DeletedAt = DateTime.UtcNow;
        version.UpdatedAt = DateTime.UtcNow;
      } else {
        _context.Remove(version);
      }
      await _context.SaveChangesAsync();
      return Ok();
    }

    private bool UserCanRead(Project project) {
      if (project.Visibility == AccessLevel.Public) { return true; }
      return project.Collaborators.Any(c => c.UserId == _userContext.UserId);
    }

    private bool UserCanEdit(Project project) {
      return project.Collaborators.Any(c => c.UserId == _userContext.UserId && c.Permissions.Contains(PermissionType.Edit.ToString()));
    }
  }

  public record CreateProjectVersionRequest(string VersionNumber, string? ReleaseNotes, string? Status);
  public record UpdateProjectVersionRequest(string? VersionNumber, string? ReleaseNotes, string? Status);
}
