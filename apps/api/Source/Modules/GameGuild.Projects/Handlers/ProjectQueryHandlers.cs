using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Projects;

/// <summary>
/// Query handlers for project operations
/// </summary>
public sealed class ProjectQueryHandlers
  : IQueryHandler<GetAllProjectsQuery, Result<IEnumerable<Project>>>,
    IQueryHandler<GetProjectByIdQuery, Result<Project?>>,
    IQueryHandler<GetProjectBySlugQuery, Result<Project?>>,
    IQueryHandler<GetProjectsByCategoryQuery, Result<IEnumerable<Project>>>,
    IQueryHandler<GetProjectsByCreatorQuery, Result<IEnumerable<Project>>>,
    IQueryHandler<GetProjectsByStatusQuery, Result<IEnumerable<Project>>>,
    IQueryHandler<GetDeletedProjectsQuery, Result<IEnumerable<Project>>>,
    IQueryHandler<SearchProjectsQuery, Result<IEnumerable<Project>>>,
    IQueryHandler<GetProjectStatisticsQuery, Result<ProjectStatistics>>,
    IQueryHandler<GetPopularProjectsQuery, Result<IEnumerable<Project>>>,
    IQueryHandler<GetRecentProjectsQuery, Result<IEnumerable<Project>>>,
    IQueryHandler<GetFeaturedProjectsQuery, Result<IEnumerable<Project>>> {
  private readonly IApplicationDbContext _context;

  private readonly IActorContextAccessor _actorContextAccessor;

  /// <summary> Gets the current actor context </summary>
  private ActorContext Actor => _actorContextAccessor.ActorContext;

  private readonly ILogger<ProjectQueryHandlers> _logger;

  public ProjectQueryHandlers(IApplicationDbContext context, IActorContextAccessor actorContextAccessor, ILogger<ProjectQueryHandlers> logger) {
    _context = context;
    _actorContextAccessor = actorContextAccessor;
    _logger = logger;
  }

  public async Task<Result<IEnumerable<Project>>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken) {
    _logger.LogDebug("Getting all projects with filters");

    var query = _context.Set<Project>().AsQueryable();

    // Apply filters
    if (!request.IncludeDeleted) { query = query.Where(p => p.DeletedAt == null); }

    if (request.Type.HasValue) { query = query.Where(p => p.Type == request.Type.Value); }

    if (request.Status.HasValue) { query = query.Where(p => p.Status == request.Status.Value); }

    if (request.Visibility.HasValue) { query = query.Where(p => p.Visibility == request.Visibility.Value); }

    if (request.CreatorId.HasValue) { query = query.Where(p => p.CreatedById == request.CreatorId.Value); }

    if (request.CategoryId.HasValue) { query = query.Where(p => p.CategoryId == request.CategoryId.Value); }

    if (!string.IsNullOrEmpty(request.SearchTerm)) {
      query = query.Where(p => p.Title.Contains(request.SearchTerm) || (p.Description != null && p.Description.Contains(request.SearchTerm)) || (p.ShortDescription != null && p.ShortDescription.Contains(request.SearchTerm)));
    }

    // Apply access control
    query = ApplyAccessControl(query);

    // Apply sorting
    query = ApplySorting(query, request.SortBy, request.SortDirection);

    // Apply pagination
    query = query.Skip(request.Skip).Take(request.Take);

    // Include related data
    query = query.Include(p => p.CreatedBy).Include(p => p.Category);

    var projects = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    return Result.Success<IEnumerable<Project>>(projects);
  }

  public async Task<Result<Project?>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken) {
    _logger.LogDebug("Getting project by ID: {ProjectId}", request.ProjectId);

    var query = _context.Set<Project>()
      .AsNoTracking()
      .Where(p => p.Id == request.ProjectId && p.DeletedAt == null);

    // Include related data if requested
    if (request.IncludeTeam) { query = query.Include(p => p.Collaborators); }

    if (request.IncludeReleases) { query = query.Include(p => p.Releases); }

    if (request.IncludeCollaborators) { query = query.Include(p => p.Collaborators); }

    // Always include basic relations
    query = query.Include(p => p.CreatedBy).Include(p => p.Category);

    // Apply access control
    query = ApplyAccessControl(query);

    var project = await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

    if (project != null && request.IncludeStatistics) {
      _logger.LogInformation("Statistics retrieval requested for project {ProjectId}, but statistics are managed separately", project.Id);
    }

    return Result.Success(project);
  }

  public async Task<Result<Project?>> Handle(GetProjectBySlugQuery request, CancellationToken cancellationToken) {
    _logger.LogDebug("Getting project by slug: {Slug}", request.Slug);

    var query = _context.Set<Project>().Where(p => p.Slug == request.Slug && p.DeletedAt == null);

    // Include related data if requested
    if (request.IncludeTeam) { query = query.Include(p => p.Collaborators); }

    if (request.IncludeReleases) { query = query.Include(p => p.Releases); }

    if (request.IncludeCollaborators) { query = query.Include(p => p.Collaborators); }

    // Always include basic relations
    query = query.Include(p => p.CreatedBy).Include(p => p.Category);

    // Apply access control
    query = ApplyAccessControl(query);

    var project = await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    return Result.Success(project);
  }

  public async Task<Result<IEnumerable<Project>>> Handle(GetProjectsByCategoryQuery request, CancellationToken cancellationToken) {
    var query = _context.Set<Project>()
      .AsNoTracking()
      .Where(p => p.CategoryId == request.CategoryId && p.DeletedAt == null);

    if (request.Status.HasValue) { query = query.Where(p => p.Status == request.Status.Value); }

    query = ApplyAccessControl(query);
    query = query.Include(p => p.Collaborators).Include(p => p.Category).OrderByDescending(p => p.CreatedAt).Skip(request.Skip).Take(request.Take);

    var projects = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    return Result.Success<IEnumerable<Project>>(projects);
  }

  public async Task<Result<IEnumerable<Project>>> Handle(GetProjectsByCreatorQuery request, CancellationToken cancellationToken) {
    var query = _context.Set<Project>()
      .AsNoTracking()
      .Where(p => p.Collaborators.Any(c => c.UserId == request.CreatorId && c.Role == ProjectRoles.Owner) && p.DeletedAt == null);

    if (request.Status.HasValue) { query = query.Where(p => p.Status == request.Status.Value); }

    query = ApplyAccessControl(query);
    query = query.Include(p => p.CreatedBy).Include(p => p.Category).OrderByDescending(p => p.CreatedAt).Skip(request.Skip).Take(request.Take);

    var projects = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    return Result.Success<IEnumerable<Project>>(projects);
  }

  public async Task<Result<IEnumerable<Project>>> Handle(GetProjectsByStatusQuery request, CancellationToken cancellationToken) {
    var query = _context.Set<Project>()
      .AsNoTracking()
      .Where(p => p.Status == request.Status && p.DeletedAt == null);

    if (request.Type.HasValue) { query = query.Where(p => p.Type == request.Type.Value); }

    query = ApplyAccessControl(query);
    query = query.Include(p => p.CreatedBy).Include(p => p.Category).OrderByDescending(p => p.CreatedAt).Skip(request.Skip).Take(request.Take);

    var projects = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    return Result.Success<IEnumerable<Project>>(projects);
  }

  public async Task<Result<IEnumerable<Project>>> Handle(GetDeletedProjectsQuery request, CancellationToken cancellationToken) {
    if (!Actor.IsAuthenticated || !Actor.IsTenantAdmin) {
      return Result.Success<IEnumerable<Project>>(Array.Empty<Project>());
    }

    IQueryable<Project> query = _context.Set<Project>()
      .AsNoTracking()
      .Where(p => p.DeletedAt != null)
      .Include(p => p.CreatedBy)
      .Include(p => p.Category);

    if (!Actor.IsSystemAdmin)
    {
      query = query.Where(project => project.TenantId == Actor.TenantId);
    }

    var projects = await query
      .OrderByDescending(p => p.DeletedAt)
      .ThenByDescending(p => p.UpdatedAt)
      .Skip(request.Skip)
      .Take(request.Take)
      .ToListAsync(cancellationToken)
      .ConfigureAwait(false);

    return Result.Success<IEnumerable<Project>>(projects);
  }

  public async Task<Result<IEnumerable<Project>>> Handle(SearchProjectsQuery request, CancellationToken cancellationToken) {
    var query = _context.Set<Project>().Where(p => p.DeletedAt == null);

    // Search term
    if (!string.IsNullOrEmpty(request.SearchTerm)) {
      query = query.Where(p => p.Title.Contains(request.SearchTerm) || (p.Description != null && p.Description.Contains(request.SearchTerm)) || (p.ShortDescription != null && p.ShortDescription.Contains(request.SearchTerm)));
    }

    // Apply filters
    if (request.Type.HasValue) query = query.Where(p => p.Type == request.Type.Value);

    if (request.CategoryId.HasValue) query = query.Where(p => p.CategoryId == request.CategoryId.Value);

    if (request.Status.HasValue) query = query.Where(p => p.Status == request.Status.Value);

    if (request.Visibility.HasValue) query = query.Where(p => p.Visibility == request.Visibility.Value);

    query = ApplyAccessControl(query);
    query = ApplySorting(query, request.SortBy, request.SortDirection);

    query = query.Include(p => p.CreatedBy).Include(p => p.Category).Skip(request.Skip).Take(request.Take);

    var projects = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    return Result.Success<IEnumerable<Project>>(projects);
  }

  public async Task<Result<ProjectStatistics>> Handle(GetProjectStatisticsQuery request, CancellationToken cancellationToken) {
    var stats = await GetProjectStatistics(request.ProjectId, cancellationToken, request.FromDate, request.ToDate).ConfigureAwait(false);
    return Result.Success(stats);
  }

  public async Task<Result<IEnumerable<Project>>> Handle(GetPopularProjectsQuery request, CancellationToken cancellationToken) {
    var query = _context.Set<Project>().Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published);

    if (request.Type.HasValue) { query = query.Where(p => p.Type == request.Type.Value); }

    // Popularity scoring: order by follower count, then feedback count, then recency
    query = ApplyAccessControl(query);

    query = query.Include(p => p.CreatedBy)
                 .Include(p => p.Category)
                 .OrderByDescending(p => p.Followers.Count)
                 .ThenByDescending(p => p.Feedbacks.Count)
                 .ThenByDescending(p => p.CreatedAt)
                 .Take(request.Take);

    var projects = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    return Result.Success<IEnumerable<Project>>(projects);
  }

  public async Task<Result<IEnumerable<Project>>> Handle(GetRecentProjectsQuery request, CancellationToken cancellationToken) {
    var query = _context.Set<Project>().Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published);

    if (request.Type.HasValue) { query = query.Where(p => p.Type == request.Type.Value); }

    query = ApplyAccessControl(query);
    query = query.Include(p => p.CreatedBy).Include(p => p.Category).OrderByDescending(p => p.CreatedAt).Take(request.Take);

    var projects = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    return Result.Success<IEnumerable<Project>>(projects);
  }

  public async Task<Result<IEnumerable<Project>>> Handle(GetFeaturedProjectsQuery request, CancellationToken cancellationToken) {
    var query = _context.Set<Project>().Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published);

    if (request.Type.HasValue) { query = query.Where(p => p.Type == request.Type.Value); }

    // Featured = projects with the most collaborators and followers (community-driven featuring)
    query = ApplyAccessControl(query);

    query = query.Include(p => p.CreatedBy)
                 .Include(p => p.Category)
                 .OrderByDescending(p => p.Collaborators.Count(c => c.IsActive))
                 .ThenByDescending(p => p.Followers.Count)
                 .ThenByDescending(p => p.CreatedAt)
                 .Take(request.Take);

    var projects = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    return Result.Success<IEnumerable<Project>>(projects);
  }

  /// <summary>
  /// Apply access control based on user context
  /// </summary>
  private IQueryable<Project> ApplyAccessControl(IQueryable<Project> query) {
    // Public projects are always visible
    var accessibleQuery = query.Where(p => p.Visibility == ContentVisibility.Public);

    if (Actor.IsAuthenticated) {
      // Authenticated users can see their own private projects
      accessibleQuery = query.Where(p => p.Visibility == ContentVisibility.Public || (p.Visibility == ContentVisibility.Private && p.Collaborators.Any(c => c.UserId == Actor.SubjectIdAsGuid)));

      // Admins can see everything
      if (Actor.IsSystemAdmin)
      {
        accessibleQuery = query;
      }
      else if (Actor.IsTenantAdmin && Actor.TenantId.HasValue)
      {
        accessibleQuery = query.Where(project => project.TenantId == Actor.TenantId.Value);
      }
    }

    return accessibleQuery;
  }

  /// <summary>
  /// Apply sorting to query
  /// </summary>
  private static IQueryable<Project> ApplySorting(IQueryable<Project> query, string? sortBy, string? sortDirection) {
    var descending = sortDirection?.ToUpperInvariant() == "DESC";

    return sortBy?.ToLowerInvariant() switch {
      "name" => descending ? query.OrderByDescending(p => p.Title) : query.OrderBy(p => p.Title),
      "createdat" => descending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
      "updatedat" => descending ? query.OrderByDescending(p => p.UpdatedAt) : query.OrderBy(p => p.UpdatedAt),
      _ => query.OrderByDescending(p => p.CreatedAt),
    };
  }

  /// <summary>
  /// Get project statistics
  /// </summary>
  private async Task<ProjectStatistics> GetProjectStatistics(Guid projectId, CancellationToken cancellationToken, DateTime? fromDate = null, DateTime? toDate = null) {
    var project = await _context.Set<Project>()
      .Include(p => p.Followers)
      .Include(p => p.Feedbacks)
      .Include(p => p.Releases)
      .Include(p => p.ProjectMetadata)
      .Include(p => p.Collaborators)
      .Include(p => p.Teams)
      .Include(p => p.JamSubmissions)
      .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken).ConfigureAwait(false);

    if (project == null)
      return new ProjectStatistics { ProjectId = projectId };

    return new ProjectStatistics {
      ProjectId = projectId,
      FollowerCount = project.Followers.Count,
      FeedbackCount = project.Feedbacks.Count,
      TotalDownloads = (project.ProjectMetadata?.DownloadCount ?? 0) + project.Releases.Sum(release => release.DownloadCount),
      ActiveTeamCount = project.Teams.Count,
      CollaboratorCount = project.Collaborators.Count(c => c.IsActive),
      ReleaseCount = project.Releases.Count,
      JamSubmissionCount = project.JamSubmissions.Count,
      AwardCount = 0,
    };
  }
}
