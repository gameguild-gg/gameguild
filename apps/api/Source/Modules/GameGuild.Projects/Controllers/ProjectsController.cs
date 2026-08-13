using System.ComponentModel.DataAnnotations;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using PermissionType = GameGuild.Identity.Authorization.PermissionType;


namespace GameGuild.Projects;

/// <summary> REST API controller for managing projects using CQRS pattern </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/projects")]
[Authorize]
public class ProjectsController : BaseApiController {
  private readonly ILogger<ProjectsController> _logger;

  private readonly IMediator _mediator;

  private readonly IActorContextAccessor _actorContextAccessor;

  private readonly IApplicationDbContext _context;

  private readonly IProjectAuthorizationService _authorizationService;

  public ProjectsController(IMediator mediator, IActorContextAccessor actorContextAccessor, IApplicationDbContext context, IProjectAuthorizationService authorizationService, ILogger<ProjectsController> logger) {
    _mediator = mediator;
    _actorContextAccessor = actorContextAccessor;
    _context = context;
    _authorizationService = authorizationService;
    _logger = logger;
  }

  /// <summary> Get all projects with filtering and pagination </summary>
  /// <remarks>
  /// Use query parameters to filter results:
  /// - `featured=true` to get featured projects
  /// - `popular=true` to get popular projects (sorted by popularity score)
  /// - `recent=true` to get recently created/updated projects
  /// - `sortBy=CreatedAt` with `sortDirection=DESC` for manual sorting
  /// </remarks>
  [HttpGet]
  [AllowAnonymous]
  public async Task<ActionResult<IEnumerable<Project>>> GetProjects(
    [FromQuery] ProjectType? type = null,
    [FromQuery] ContentStatus? status = null,
    [FromQuery] ContentVisibility? visibility = null,
    [FromQuery] Guid? creatorId = null,
    [FromQuery] Guid? categoryId = null,
    [FromQuery] string? searchTerm = null,
    [FromQuery] bool? featured = null,
    [FromQuery] bool? popular = null,
    [FromQuery] bool? recent = null,
    [FromQuery] bool currentTenantOnly = false,
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50,
    [FromQuery] string? sortBy = "CreatedAt",
    [FromQuery] string? sortDirection = "DESC"
  ) {
    var query = new GetAllProjectsQuery {
      Type = type,
      Status = status,
      Visibility = visibility,
      CreatorId = creatorId,
      CategoryId = categoryId,
      SearchTerm = searchTerm,
      Featured = featured,
      Popular = popular,
      Recent = recent,
      CurrentTenantOnly = currentTenantOnly,
      Skip = skip,
      Take = Math.Min(take, 100), // Limit max items
      SortBy = sortBy,
      SortDirection = sortDirection,
    };

    var projects = await _mediator.Send(query).ConfigureAwait(false);

    return projects.IsSuccess ? Ok(projects.Value) : ToActionResult(projects);
  }

  /// <summary> Get project by ID </summary>
  [HttpGet("{id:guid}")]
  [AllowAnonymous]
  public async Task<ActionResult<Project>> GetProject(Guid id, [FromQuery] bool includeTeam = true, [FromQuery] bool includeReleases = true, [FromQuery] bool includeCollaborators = true, [FromQuery] bool includeStatistics = false) {
    var query = new GetProjectByIdQuery { ProjectId = id, IncludeTeam = includeTeam, IncludeReleases = includeReleases, IncludeCollaborators = includeCollaborators, IncludeStatistics = includeStatistics };

    var project = await _mediator.Send(query).ConfigureAwait(false);

    if (project.IsFailure) return ToActionResult(Result.Failure<Project>(project.Error));
    if (project.Value == null) { return NotFound(); }

    return Ok(project.Value);
  }

  /// <summary> Get project by slug </summary>
  [HttpGet("slug/{slug}")]
  [AllowAnonymous]
  public async Task<ActionResult<Project>> GetProjectBySlug(string slug, [FromQuery] bool includeTeam = true, [FromQuery] bool includeReleases = true, [FromQuery] bool includeCollaborators = true) {
    var query = new GetProjectBySlugQuery { Slug = slug, IncludeTeam = includeTeam, IncludeReleases = includeReleases, IncludeCollaborators = includeCollaborators };

    var project = await _mediator.Send(query).ConfigureAwait(false);

    if (project.IsFailure) return ToActionResult(Result.Failure<Project>(project.Error));
    if (project.Value == null) { return NotFound(); }

    return Ok(project.Value);
  }

  /// <summary> Create a new project </summary>
  [HttpPost]
  public async Task<ActionResult<Project>> CreateProject([FromBody] CreateProjectRequest request) {
    if (!ModelState.IsValid) { return BadRequest(ModelState); }

    var command = new CreateProjectCommand {
      Title = request.Title,
      Description = request.Description,
      ShortDescription = request.ShortDescription,
      ImageUrl = request.ImageUrl,
      RepositoryUrl = request.RepositoryUrl,
      WebsiteUrl = request.WebsiteUrl,
      DownloadUrl = request.DownloadUrl,
      Type = (GameGuild.ProjectType)request.Type,
      CreatedById = _actorContextAccessor.ActorContext.SubjectIdAsGuid ?? Guid.Empty,
      CategoryId = request.CategoryId,
      Visibility = request.Visibility,
      Status = request.Status,
      Tags = request.Tags,
      TenantId = _actorContextAccessor.ActorContext.TenantId,
    };

    var result = await _mediator.Send(command).ConfigureAwait(false);

    if (result.IsSuccess)
      return CreatedAtAction(nameof(GetProject), new { id = result.Value.Id }, result.Value);

    return ToActionResult(result);
  }

  /// <summary> Update an existing project </summary>
  [HttpPut("{id:guid}")]
  public async Task<ActionResult<Project>> UpdateProject(Guid id, [FromBody] UpdateProjectRequest request) {
    var command = new UpdateProjectCommand {
      ProjectId = id,
      Title = request.Title,
      Description = request.Description,
      ShortDescription = request.ShortDescription,
      ImageUrl = request.ImageUrl,
      RepositoryUrl = request.RepositoryUrl,
      WebsiteUrl = request.WebsiteUrl,
      DownloadUrl = request.DownloadUrl,
      Type = (GameGuild.ProjectType?)request.Type,
      CategoryId = request.CategoryId,
      Visibility = request.Visibility,
      Status = request.Status,
      Tags = request.Tags,
      UpdatedBy = _actorContextAccessor.ActorContext.SubjectIdAsGuid ?? Guid.Empty,
    };

    var result = await _mediator.Send(command).ConfigureAwait(false);

    return ToActionResult(result);
  }

  /// <summary> Delete a project </summary>
  [HttpDelete("{id:guid}")]
  public async Task<ActionResult<bool>> DeleteProject(Guid id, [FromQuery] bool softDelete = true, [FromQuery] string? reason = null) {
    var command = new DeleteProjectCommand { ProjectId = id, DeletedBy = _actorContextAccessor.ActorContext.SubjectIdAsGuid ?? Guid.Empty, SoftDelete = softDelete, Reason = reason };

    var result = await _mediator.Send(command).ConfigureAwait(false);

    return ToActionResult(result);
  }

  /// <summary> Publish a project </summary>
  [HttpPost("{id:guid}:publish")]
  public async Task<ActionResult<Project>> PublishProject(Guid id) {
    var command = new PublishProjectCommand { ProjectId = id, PublishedBy = _actorContextAccessor.ActorContext.SubjectIdAsGuid ?? Guid.Empty };

    var result = await _mediator.Send(command).ConfigureAwait(false);

    return ToActionResult(result);
  }

  /// <summary> Unpublish a project </summary>
  [HttpPost("{id:guid}:unpublish")]
  public async Task<ActionResult<Project>> UnpublishProject(Guid id) {
    var command = new UnpublishProjectCommand { ProjectId = id, UnpublishedBy = _actorContextAccessor.ActorContext.SubjectIdAsGuid ?? Guid.Empty };

    var result = await _mediator.Send(command).ConfigureAwait(false);

    return ToActionResult(result);
  }

  /// <summary> Archive a project </summary>
  [HttpPost("{id:guid}:archive")]
  public async Task<ActionResult<Project>> ArchiveProject(Guid id) {
    var command = new ArchiveProjectCommand { ProjectId = id, ArchivedBy = _actorContextAccessor.ActorContext.SubjectIdAsGuid ?? Guid.Empty };

    var result = await _mediator.Send(command).ConfigureAwait(false);

    return ToActionResult(result);
  }

  /// <summary> Search projects </summary>
  [HttpGet("search")]
  [AllowAnonymous]
  public async Task<ActionResult<IEnumerable<Project>>> SearchProjects(
    [FromQuery] string searchTerm,
    [FromQuery] ProjectType? type = null,
    [FromQuery] Guid? categoryId = null,
    [FromQuery] ContentStatus? status = null,
    [FromQuery] ContentVisibility? visibility = null,
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50,
    [FromQuery] string? sortBy = "Relevance",
    [FromQuery] string? sortDirection = "DESC"
  ) {
    var query = new SearchProjectsQuery { SearchTerm = searchTerm, Type = type, CategoryId = categoryId, Status = status, Visibility = visibility, Skip = skip, Take = Math.Min(take, 100), SortBy = sortBy, SortDirection = sortDirection };

    var projects = await _mediator.Send(query).ConfigureAwait(false);

    return projects.IsSuccess ? Ok(projects.Value) : ToActionResult(projects);
  }

  /// <summary> Get popular projects </summary>
  [HttpGet("popular")]
  [AllowAnonymous]
  public async Task<ActionResult<IEnumerable<Project>>> GetPopularProjects([FromQuery] ProjectType? type = null, [FromQuery] int take = 10) {
    var query = new GetPopularProjectsQuery { Type = type, Take = Math.Min(take, 50) };

    var projects = await _mediator.Send(query).ConfigureAwait(false);

    return projects.IsSuccess ? Ok(projects.Value) : ToActionResult(projects);
  }

  /// <summary> Get recent projects </summary>
  [HttpGet("recent")]
  [AllowAnonymous]
  public async Task<ActionResult<IEnumerable<Project>>> GetRecentProjects([FromQuery] ProjectType? type = null, [FromQuery] int take = 10) {
    var query = new GetRecentProjectsQuery { Type = type, Take = Math.Min(take, 50) };

    var projects = await _mediator.Send(query).ConfigureAwait(false);

    return projects.IsSuccess ? Ok(projects.Value) : ToActionResult(projects);
  }

  /// <summary> Get featured projects </summary>
  [HttpGet("featured")]
  [AllowAnonymous]
  public async Task<ActionResult<IEnumerable<Project>>> GetFeaturedProjects([FromQuery] ProjectType? type = null, [FromQuery] int take = 10) {
    var query = new GetFeaturedProjectsQuery { Type = type, Take = Math.Min(take, 50) };

    var projects = await _mediator.Send(query).ConfigureAwait(false);

    return projects.IsSuccess ? Ok(projects.Value) : ToActionResult(projects);
  }

  /// <summary> Get project statistics </summary>
  [HttpGet("{id:guid}/statistics")]
  [AllowAnonymous]
  public async Task<ActionResult<ProjectStatistics>> GetProjectStatistics(Guid id, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null) {
    var query = new GetProjectStatisticsQuery { ProjectId = id, FromDate = fromDate, ToDate = toDate };

    var statistics = await _mediator.Send(query).ConfigureAwait(false);

    return statistics.IsSuccess ? Ok(statistics.Value) : ToActionResult(statistics);
  }

  /// <summary> Get projects by category </summary>
  [HttpGet("category/{categoryId:guid}")]
  [AllowAnonymous]
  public async Task<ActionResult<IEnumerable<Project>>> GetProjectsByCategory(Guid categoryId, [FromQuery] ContentStatus? status = null, [FromQuery] int skip = 0, [FromQuery] int take = 50) {
    var query = new GetProjectsByCategoryQuery { CategoryId = categoryId, Status = status, Skip = skip, Take = Math.Min(take, 100) };

    var projects = await _mediator.Send(query).ConfigureAwait(false);

    return projects.IsSuccess ? Ok(projects.Value) : ToActionResult(projects);
  }

  /// <summary> Get projects by creator </summary>
  [HttpGet("creator/{creatorId:guid}")]
  [AllowAnonymous]
  public async Task<ActionResult<IEnumerable<Project>>> GetProjectsByCreator(Guid creatorId, [FromQuery] ContentStatus? status = null, [FromQuery] int skip = 0, [FromQuery] int take = 50) {
    var query = new GetProjectsByCreatorQuery { CreatorId = creatorId, Status = status, Skip = skip, Take = Math.Min(take, 100) };

    var projects = await _mediator.Send(query).ConfigureAwait(false);

    return projects.IsSuccess ? Ok(projects.Value) : ToActionResult(projects);
  }

  /// <summary> Get available role templates for projects </summary>
  [HttpGet("role-templates")]
  [AllowAnonymous]
  public ActionResult<IEnumerable<object>> GetProjectRoleTemplates() {
    // Return predefined project roles
    var roleTemplates = new[] {
      new { Name = "Owner", Description = "Full access to project", Permissions = new[] { PermissionType.Read, PermissionType.Edit, PermissionType.Delete, PermissionType.Create, PermissionType.Share, PermissionType.Comment, PermissionType.Reply, PermissionType.Review, PermissionType.Approve, PermissionType.Publish, PermissionType.Archive, PermissionType.Restore } },
      new { Name = "Admin", Description = "Administrative access", Permissions = new[] { PermissionType.Read, PermissionType.Edit, PermissionType.Delete, PermissionType.Create, PermissionType.Share, PermissionType.Comment, PermissionType.Reply, PermissionType.Review, PermissionType.Approve, PermissionType.Publish, PermissionType.Archive, PermissionType.Restore } },
      new { Name = "Editor", Description = "Can edit project content", Permissions = new[] { PermissionType.Read, PermissionType.Edit, PermissionType.Create, PermissionType.Comment, PermissionType.Reply, PermissionType.Share, PermissionType.Review, PermissionType.Approve, PermissionType.Publish } },
      new { Name = "Viewer", Description = "Read-only access", Permissions = new[] { PermissionType.Read, PermissionType.Comment } },
      new { Name = "Collaborator", Description = "Can collaborate on project", Permissions = new[] { PermissionType.Read, PermissionType.Edit, PermissionType.Comment, PermissionType.Reply, PermissionType.Share, PermissionType.Create } }
    };

    return Ok(roleTemplates);
  }

  /// <summary> Get current user's project invitations </summary>
  [HttpGet("my-invitations")]
  public async Task<ActionResult<IEnumerable<ProjectInvitationDto>>> GetMyProjectInvitations() {
    var actor = _actorContextAccessor.ActorContext;
    var userId = actor.SubjectIdAsGuid;
    if (!userId.HasValue) return Unauthorized();

    var invitations = await _context.Set<ProjectInvitation>()
      .AsNoTracking()
      .Include(invitation => invitation.Project)
      .Where(invitation => invitation.InvitedUserId == userId.Value && invitation.Status == ProjectInvitationStatus.Pending)
      .OrderByDescending(invitation => invitation.InvitedAt)
      .ToListAsync()
      .ConfigureAwait(false);

    return Ok(invitations.Select(ProjectInvitationDto.FromInvitation).ToList());
  }

  /// <summary> Get permissions for a specific role </summary>
  [HttpGet("roles/{roleName}/permissions")]
  [AllowAnonymous]
  public ActionResult<IEnumerable<PermissionType>> GetRolePermissions(string roleName) {
    PermissionType[] permissions = roleName.ToLower() switch {
      "owner" => [PermissionType.Read, PermissionType.Edit, PermissionType.Delete, PermissionType.Create, PermissionType.Share, PermissionType.Comment, PermissionType.Reply, PermissionType.Review, PermissionType.Approve, PermissionType.Publish, PermissionType.Archive, PermissionType.Restore],
      "admin" => [PermissionType.Read, PermissionType.Edit, PermissionType.Delete, PermissionType.Create, PermissionType.Share, PermissionType.Comment, PermissionType.Reply, PermissionType.Review, PermissionType.Approve, PermissionType.Publish, PermissionType.Archive, PermissionType.Restore],
      "editor" => [PermissionType.Read, PermissionType.Edit, PermissionType.Create, PermissionType.Comment, PermissionType.Reply, PermissionType.Share, PermissionType.Review, PermissionType.Approve, PermissionType.Publish],
      "viewer" => [PermissionType.Read, PermissionType.Comment],
      "collaborator" => [PermissionType.Read, PermissionType.Edit, PermissionType.Comment, PermissionType.Reply, PermissionType.Share, PermissionType.Create],
      _ => []
    };

    if (permissions.Length == 0) {
      return BadRequest($"Role '{roleName}' not found");
    }

    return Ok(permissions);
  }

  /// <summary> Accept a project invitation </summary>
  [HttpPost("invitations/{invitationToken}:accept")]
  public async Task<ActionResult<ProjectInvitationDto>> AcceptProjectInvitation(string invitationToken) {
    var invitation = await GetRespondableInvitation(invitationToken).ConfigureAwait(false);
    if (invitation == null) return NotFound(new { Message = "Invitation not found" });
    if (!invitation.CanRespond) return Conflict(new { Message = "Invitation is no longer pending or has expired" });

    var actor = _actorContextAccessor.ActorContext;
    var userId = actor.SubjectIdAsGuid;
    if (!userId.HasValue || invitation.InvitedUserId != userId.Value) return Forbid();

    invitation.Accept();

    var collaborator = await _context.Set<ProjectCollaborator>()
      .FirstOrDefaultAsync(c => c.ProjectId == invitation.ProjectId && c.UserId == userId.Value)
      .ConfigureAwait(false);

    if (collaborator == null) {
      collaborator = new ProjectCollaborator {
        ProjectId = invitation.ProjectId,
        UserId = userId.Value,
        Role = invitation.Role,
        Permissions = invitation.Permissions,
        IsActive = true,
        JoinedAt = SystemClock.UtcNow
      };
      _context.Set<ProjectCollaborator>().Add(collaborator);
    }
    else {
      collaborator.Role = invitation.Role;
      collaborator.Permissions = invitation.Permissions;
      collaborator.IsActive = true;
      collaborator.LeftAt = null;
    }

    await _context.SaveChangesAsync().ConfigureAwait(false);

    return Ok(ProjectInvitationDto.FromInvitation(invitation));
  }

  /// <summary> Decline a project invitation </summary>
  [HttpPost("invitations/{invitationToken}:decline")]
  public async Task<ActionResult<ProjectInvitationDto>> DeclineProjectInvitation(string invitationToken) {
    var invitation = await GetRespondableInvitation(invitationToken).ConfigureAwait(false);
    if (invitation == null) return NotFound(new { Message = "Invitation not found" });
    if (!invitation.CanRespond) return Conflict(new { Message = "Invitation is no longer pending or has expired" });

    var actor = _actorContextAccessor.ActorContext;
    var userId = actor.SubjectIdAsGuid;
    if (!userId.HasValue || invitation.InvitedUserId != userId.Value) return Forbid();

    invitation.Decline();
    await _context.SaveChangesAsync().ConfigureAwait(false);

    return Ok(ProjectInvitationDto.FromInvitation(invitation));
  }

  /// <summary> Get project collaborators </summary>
  [HttpGet("{id:guid}/collaborators")]
  public async Task<ActionResult<IEnumerable<CollaboratorDto>>> GetProjectCollaborators(Guid id) {
    if (!await _authorizationService.HasPermissionAsync(id, PermissionType.Read).ConfigureAwait(false)) return NotFound();
    var collaborators = await _context.Set<ProjectCollaborator>()
      .Where(c => c.ProjectId == id && c.IsActive)
      .Include(c => c.User)
      .OrderBy(c => c.JoinedAt)
      .Select(c => new CollaboratorDto {
        Id = c.Id,
        UserId = c.UserId,
        UserName = c.User != null ? c.User.Name : "Unknown",
        Role = c.Role,
        Permissions = c.Permissions,
        JoinedAt = c.JoinedAt,
        IsActive = c.IsActive
      })
      .ToListAsync().ConfigureAwait(false);

    return Ok(collaborators);
  }

  /// <summary> Add project collaborator </summary>
  [HttpPost("{id:guid}/collaborators")]
  public async Task<ActionResult<CollaboratorDto>> AddProjectCollaborator(Guid id, [FromBody] AddProjectCollaboratorRequest request) {
    if (!await _authorizationService.HasPermissionAsync(id, PermissionType.Edit).ConfigureAwait(false)) return NotFound();
    var project = await _context.Set<Project>().FindAsync(id).ConfigureAwait(false);
    if (project == null) return NotFound();

    var actor = _actorContextAccessor.ActorContext;
    var userId = actor.SubjectIdAsGuid ?? Guid.Empty;

    // Check if user is already a collaborator
    var exists = await _context.Set<ProjectCollaborator>()
      .AnyAsync(c => c.ProjectId == id && c.UserId == request.UserId && c.IsActive).ConfigureAwait(false);
    if (exists) return Conflict(new { Message = "User is already a collaborator" });

    var collaborator = new ProjectCollaborator {
      ProjectId = id,
      UserId = request.UserId,
      Role = request.Role ?? "Collaborator",
      Permissions = request.Permissions ?? "read,comment",
      IsActive = true,
      JoinedAt = SystemClock.UtcNow
    };

    _context.Set<ProjectCollaborator>().Add(collaborator);
    await _context.SaveChangesAsync().ConfigureAwait(false);

    _logger.LogInformation("User {AdminId} added collaborator {UserId} to project {ProjectId} with role {Role}", userId, request.UserId, id, collaborator.Role);

    return CreatedAtAction(nameof(GetProjectCollaborators), new { id }, new CollaboratorDto {
      Id = collaborator.Id,
      UserId = collaborator.UserId,
      Role = collaborator.Role,
      Permissions = collaborator.Permissions,
      JoinedAt = collaborator.JoinedAt,
      IsActive = collaborator.IsActive
    });
  }

  /// <summary> Update project collaborator </summary>
  [HttpPut("{id:guid}/collaborators/{collaboratorId:guid}")]
  public async Task<ActionResult<CollaboratorDto>> UpdateProjectCollaborator(Guid id, Guid collaboratorId, [FromBody] UpdateProjectCollaboratorRequest request) {
    if (!await _authorizationService.HasPermissionAsync(id, PermissionType.Edit).ConfigureAwait(false)) return NotFound();
    var collaborator = await _context.Set<ProjectCollaborator>()
      .FirstOrDefaultAsync(c => c.Id == collaboratorId && c.ProjectId == id).ConfigureAwait(false);
    if (collaborator == null) return NotFound();

    var userId = _actorContextAccessor.ActorContext.SubjectIdAsGuid ?? Guid.Empty;

    if (request.Role != null) collaborator.Role = request.Role;
    if (request.Permissions != null) collaborator.Permissions = request.Permissions;

    await _context.SaveChangesAsync().ConfigureAwait(false);

    _logger.LogInformation("User {AdminId} updated collaborator {CollaboratorId} on project {ProjectId}", userId, collaboratorId, id);

    return Ok(new CollaboratorDto {
      Id = collaborator.Id,
      UserId = collaborator.UserId,
      Role = collaborator.Role,
      Permissions = collaborator.Permissions,
      JoinedAt = collaborator.JoinedAt,
      IsActive = collaborator.IsActive
    });
  }

  /// <summary> Remove project collaborator </summary>
  [HttpDelete("{id:guid}/collaborators/{collaboratorId:guid}")]
  public async Task<ActionResult> RemoveProjectCollaborator(Guid id, Guid collaboratorId) {
    if (!await _authorizationService.HasPermissionAsync(id, PermissionType.Edit).ConfigureAwait(false)) return NotFound();
    var collaborator = await _context.Set<ProjectCollaborator>()
      .FirstOrDefaultAsync(c => c.Id == collaboratorId && c.ProjectId == id).ConfigureAwait(false);
    if (collaborator == null) return NotFound();

    var userId = _actorContextAccessor.ActorContext.SubjectIdAsGuid ?? Guid.Empty;

    // Soft-delete: mark as inactive
    collaborator.IsActive = false;
    collaborator.LeftAt = SystemClock.UtcNow;
    await _context.SaveChangesAsync().ConfigureAwait(false);

    _logger.LogInformation("User {AdminId} removed collaborator {CollaboratorId} from project {ProjectId}", userId, collaboratorId, id);

    return NoContent();
  }

  /// <summary> Share project with a user by assigning a role </summary>
  [HttpPost("{id:guid}:share")]
  public async Task<ActionResult<CollaboratorDto>> ShareProject(Guid id, [FromBody] ShareProjectRequest request) {
    if (!await _authorizationService.HasPermissionAsync(id, PermissionType.Share).ConfigureAwait(false)) return NotFound();
    var project = await _context.Set<Project>().FindAsync(id).ConfigureAwait(false);
    if (project == null) return NotFound();

    var actor = _actorContextAccessor.ActorContext;
    var userId = actor.SubjectIdAsGuid ?? Guid.Empty;
    // Check if already shared
    var existing = await _context.Set<ProjectCollaborator>()
      .FirstOrDefaultAsync(c => c.ProjectId == id && c.UserId == request.UserId).ConfigureAwait(false);

    if (existing != null) {
      // Re-activate if previously removed, or update role
      existing.IsActive = true;
      existing.Role = request.Role ?? "Viewer";
      existing.Permissions = request.Permissions ?? "read";
      existing.LeftAt = null;
    } else {
      var collaborator = new ProjectCollaborator {
        ProjectId = id,
        UserId = request.UserId,
        Role = request.Role ?? "Viewer",
        Permissions = request.Permissions ?? "read",
        IsActive = true,
        JoinedAt = SystemClock.UtcNow
      };
      _context.Set<ProjectCollaborator>().Add(collaborator);
    }

    await _context.SaveChangesAsync().ConfigureAwait(false);

    _logger.LogInformation("User {AdminId} shared project {ProjectId} with user {TargetUserId} as {Role}", userId, id, request.UserId, request.Role ?? "Viewer");

    return Ok(new { Message = "Project shared", ProjectId = id, UserId = request.UserId, Role = request.Role ?? "Viewer" });
  }

  /// <summary> Invite a user to collaborate on a project without granting access until acceptance </summary>
  [HttpPost("{id:guid}/invitations")]
  public async Task<ActionResult<ProjectInvitationDto>> InviteProjectCollaborator(Guid id, [FromBody] InviteProjectCollaboratorRequest request) {
    if (!await _authorizationService.HasPermissionAsync(id, PermissionType.Share).ConfigureAwait(false)) return NotFound();
    var project = await _context.Set<Project>().FindAsync(id).ConfigureAwait(false);
    if (project == null) return NotFound();

    var actor = _actorContextAccessor.ActorContext;
    var userId = actor.SubjectIdAsGuid ?? Guid.Empty;
    if (!request.UserId.HasValue && string.IsNullOrWhiteSpace(request.Email)) {
      return BadRequest(new { Message = "Provide either a user id or an email address." });
    }

    var invitation = new ProjectInvitation {
      ProjectId = id,
      InvitedUserId = request.UserId,
      InvitedEmail = request.Email?.Trim(),
      InvitedByUserId = userId,
      Role = request.Role ?? "Viewer",
      Permissions = request.Permissions ?? "read",
      ExpiresAt = request.ExpiresAt,
      Token = Guid.NewGuid().ToString("N"),
    };

    _context.Set<ProjectInvitation>().Add(invitation);
    await _context.SaveChangesAsync().ConfigureAwait(false);

    return CreatedAtAction(nameof(GetMyProjectInvitations), new { }, ProjectInvitationDto.FromInvitation(invitation));
  }

  private async Task<ProjectInvitation?> GetRespondableInvitation(string invitationToken) {
    return await _context.Set<ProjectInvitation>()
      .Include(invitation => invitation.Project)
      .FirstOrDefaultAsync(invitation => invitation.Token == invitationToken)
      .ConfigureAwait(false);
  }
}

/// <summary> Request DTOs for REST API </summary>
public sealed record CreateProjectRequest {
  [Required(ErrorMessage = "Title is required")]
  [StringLength(255, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 255 characters")]
  public string Title { get; init; } = string.Empty;

  [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")] public string? Description { get; init; }

  [StringLength(500, ErrorMessage = "Short description cannot exceed 500 characters")] public string? ShortDescription { get; init; }

  [Url(ErrorMessage = "Image URL must be a valid URL")] public string? ImageUrl { get; init; }

  [Url(ErrorMessage = "Repository URL must be a valid URL")] public string? RepositoryUrl { get; init; }

  [Url(ErrorMessage = "Website URL must be a valid URL")] public string? WebsiteUrl { get; init; }

  [Url(ErrorMessage = "Download URL must be a valid URL")] public string? DownloadUrl { get; init; }

  public ProjectType Type { get; init; } = ProjectType.Game;

  public Guid? CategoryId { get; init; }

  public ContentVisibility Visibility { get; init; } = ContentVisibility.Public;

  public ContentStatus Status { get; init; } = ContentStatus.Draft;

  public List<string>? Tags { get; init; }
}

public sealed record UpdateProjectRequest {
  public string? Title { get; init; }

  public string? Description { get; init; }

  public string? ShortDescription { get; init; }

  public string? ImageUrl { get; init; }

  public string? RepositoryUrl { get; init; }

  public string? WebsiteUrl { get; init; }

  public string? DownloadUrl { get; init; }

  public ProjectType? Type { get; init; }

  public Guid? CategoryId { get; init; }

  public ContentVisibility? Visibility { get; init; }

  public ContentStatus? Status { get; init; }

  public List<string>? Tags { get; init; }
}

/// <summary> DTO for collaborator responses </summary>
public sealed record CollaboratorDto {
  public Guid Id { get; init; }
  public Guid UserId { get; init; }
  public string UserName { get; init; } = string.Empty;
  public string Role { get; init; } = string.Empty;
  public string Permissions { get; init; } = string.Empty;
  public DateTime JoinedAt { get; init; }
  public bool IsActive { get; init; }
}

/// <summary> Request to add a project collaborator by ID </summary>
public sealed record AddProjectCollaboratorRequest {
  [Required] public Guid UserId { get; init; }
  public string? Role { get; init; }
  public string? Permissions { get; init; }
}

/// <summary> Request to update a project collaborator </summary>
public sealed record UpdateProjectCollaboratorRequest {
  public string? Role { get; init; }
  public string? Permissions { get; init; }
}

/// <summary> Request to share a project </summary>
public sealed record ShareProjectRequest {
  [Required] public Guid UserId { get; init; }
  public string? Role { get; init; }
  public string? Permissions { get; init; }
}

public sealed record InviteProjectCollaboratorRequest {
  public Guid? UserId { get; init; }
  public string? Email { get; init; }
  public string? Role { get; init; }
  public string? Permissions { get; init; }
  public DateTime? ExpiresAt { get; init; }
}

public sealed record ProjectInvitationDto(
  Guid Id,
  Guid ProjectId,
  string ProjectTitle,
  Guid? InvitedUserId,
  string? InvitedEmail,
  Guid InvitedByUserId,
  string Role,
  string Permissions,
  string Token,
  ProjectInvitationStatus Status,
  DateTime InvitedAt,
  DateTime? ExpiresAt,
  DateTime? RespondedAt
) {
  public static ProjectInvitationDto FromInvitation(ProjectInvitation invitation)
    => new(
      invitation.Id,
      invitation.ProjectId,
      invitation.Project?.Title ?? string.Empty,
      invitation.InvitedUserId,
      invitation.InvitedEmail,
      invitation.InvitedByUserId,
      invitation.Role,
      invitation.Permissions,
      invitation.Token,
      invitation.Status,
      invitation.InvitedAt,
      invitation.ExpiresAt,
      invitation.RespondedAt);
}
