using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Contents;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Projects;

/// <summary>
/// Query handlers for project operations
/// </summary>
public class ProjectQueryHandlers :
  IRequestHandler<GetAllProjectsQuery, IEnumerable<Project>>,
  IRequestHandler<GetProjectByIdQuery, Project?>,
  IRequestHandler<GetProjectBySlugQuery, Project?>,
  IRequestHandler<GetProjectsByCategoryQuery, IEnumerable<Project>>,
  IRequestHandler<GetProjectsByCreatorQuery, IEnumerable<Project>>,
  IRequestHandler<GetProjectsByStatusQuery, IEnumerable<Project>>,
  IRequestHandler<GetDeletedProjectsQuery, IEnumerable<Project>>,
  IRequestHandler<SearchProjectsQuery, IEnumerable<Project>>,
  IRequestHandler<GetProjectStatisticsQuery, ProjectStatistics>,
  IRequestHandler<GetPopularProjectsQuery, IEnumerable<Project>>,
  IRequestHandler<GetRecentProjectsQuery, IEnumerable<Project>>,
  IRequestHandler<GetFeaturedProjectsQuery, IEnumerable<Project>> {
  private readonly ApplicationDbContext _context;
  private readonly IUserContext _userContext;
  private readonly ITenantContext _tenantContext;
  private readonly ILogger<ProjectQueryHandlers> _logger;

  public ProjectQueryHandlers(
    ApplicationDbContext context,
    IUserContext userContext,
    ITenantContext tenantContext,
    ILogger<ProjectQueryHandlers> logger
  ) {
    _context = context;
    _userContext = userContext;
    _tenantContext = tenantContext;
    _logger = logger;
  }

  public async Task<IEnumerable<Project>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken) {
    try {
      _logger.LogDebug("Getting all projects with filters");

      var query = _context.Projects.AsQueryable();

      // Apply filters
      if (!request.IncludeDeleted) { query = query.Where(p => p.DeletedAt == null); }

      if (request.Type.HasValue) { query = query.Where(p => p.Type == request.Type.Value); }

      if (request.Status.HasValue) { query = query.Where(p => p.Status == request.Status.Value); }

      if (request.Visibility.HasValue) { query = query.Where(p => p.Visibility == request.Visibility.Value); }

      if (request.CreatorId.HasValue) { query = query.Where(p => p.CreatedById == request.CreatorId.Value); }

      if (request.CategoryId.HasValue) { query = query.Where(p => p.CategoryId == request.CategoryId.Value); }

      if (!string.IsNullOrEmpty(request.SearchTerm)) {
        query = query.Where(p =>
                              p.Title.Contains(request.SearchTerm) ||
                              (p.Description != null && p.Description.Contains(request.SearchTerm)) ||
                              (p.ShortDescription != null && p.ShortDescription.Contains(request.SearchTerm))
        );
      }

      // Apply access control
      query = ApplyAccessControl(query);

      // Apply sorting
      query = ApplySorting(query, request.SortBy, request.SortDirection);

      // Apply pagination
      query = query.Skip(request.Skip).Take(request.Take);

      // Include related data
      query = query.Include(p => p.CreatedBy)
                   .Include(p => p.Category);

      return await query.ToListAsync(cancellationToken);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting all projects");

      return Enumerable.Empty<Project>();
    }
  }

  public async Task<Project?> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken) {
    try {
      _logger.LogDebug("Getting project by ID: {ProjectId}", request.ProjectId);

      var query = _context.Projects
                          .Where(p => p.Id == request.ProjectId && p.DeletedAt == null);

      // Include related data if requested
      if (request.IncludeTeam) { query = query.Include(p => p.Collaborators); }

      if (request.IncludeReleases) { query = query.Include(p => p.Releases); }

      if (request.IncludeCollaborators) { query = query.Include(p => p.Collaborators); }

      // Always include basic relations
      query = query.Include(p => p.CreatedBy)
                   .Include(p => p.Category);

      // Apply access control
      query = ApplyAccessControl(query);

      var project = await query.FirstOrDefaultAsync(cancellationToken);

      if (project != null && request.IncludeStatistics) {
        // Note: Statistics are retrieved separately via GetProjectStatisticsQuery
        // They are not directly attached to the Project entity
        _logger.LogInformation("Statistics retrieval requested for project {ProjectId}, but statistics are managed separately", project.Id);
      }

      return project;
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting project by ID: {ProjectId}", request.ProjectId);

      return null;
    }
  }

  public async Task<Project?> Handle(GetProjectBySlugQuery request, CancellationToken cancellationToken) {
    try {
      _logger.LogDebug("Getting project by slug: {Slug}", request.Slug);

      var query = _context.Projects
                          .Where(p => p.Slug == request.Slug && p.DeletedAt == null);

      // Include related data if requested
      if (request.IncludeTeam) { query = query.Include(p => p.Collaborators); }

      if (request.IncludeReleases) { query = query.Include(p => p.Releases); }

      if (request.IncludeCollaborators) { query = query.Include(p => p.Collaborators); }

      // Always include basic relations
      query = query.Include(p => p.CreatedBy)
                   .Include(p => p.Category);

      // Apply access control
      query = ApplyAccessControl(query);

      return await query.FirstOrDefaultAsync(cancellationToken);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting project by slug: {Slug}", request.Slug);

      return null;
    }
  }

  public async Task<IEnumerable<Project>> Handle(GetProjectsByCategoryQuery request, CancellationToken cancellationToken) {
    try {
      var query = _context.Projects
                          .Where(p => p.CategoryId == request.CategoryId && p.DeletedAt == null);

      if (request.Status.HasValue) { query = query.Where(p => p.Status == request.Status.Value); }

      query = ApplyAccessControl(query);
      query = query.Include(p => p.Collaborators)
                   .Include(p => p.Category)
                   .OrderByDescending(p => p.CreatedAt)
                   .Skip(request.Skip)
                   .Take(request.Take);

      return await query.ToListAsync(cancellationToken);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting projects by category: {CategoryId}", request.CategoryId);

      return Enumerable.Empty<Project>();
    }
  }

  public async Task<IEnumerable<Project>> Handle(GetProjectsByCreatorQuery request, CancellationToken cancellationToken) {
    try {
      var query = _context.Projects
                          .Where(p => p.Collaborators.Any(c => c.UserId == request.CreatorId && c.Role == ProjectRoles.Owner) && p.DeletedAt == null);

      if (request.Status.HasValue) { query = query.Where(p => p.Status == request.Status.Value); }

      query = ApplyAccessControl(query);
      query = query.Include(p => p.CreatedBy)
                   .Include(p => p.Category)
                   .OrderByDescending(p => p.CreatedAt)
                   .Skip(request.Skip)
                   .Take(request.Take);

      return await query.ToListAsync(cancellationToken);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting projects by creator: {CreatorId}", request.CreatorId);

      return Enumerable.Empty<Project>();
    }
  }

  public async Task<IEnumerable<Project>> Handle(GetProjectsByStatusQuery request, CancellationToken cancellationToken) {
    try {
      var query = _context.Projects
                          .Where(p => p.Status == request.Status && p.DeletedAt == null);

      if (request.Type.HasValue) { query = query.Where(p => p.Type == request.Type.Value); }

      query = ApplyAccessControl(query);
      query = query.Include(p => p.CreatedBy)
                   .Include(p => p.Category)
                   .OrderByDescending(p => p.CreatedAt)
                   .Skip(request.Skip)
                   .Take(request.Take);

      return await query.ToListAsync(cancellationToken);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting projects by status: {Status}", request.Status);

      return Enumerable.Empty<Project>();
    }
  }

  public Task<IEnumerable<Project>> Handle(GetDeletedProjectsQuery request, CancellationToken cancellationToken) {
    try {
      // Deleted projects are restricted
      return Task.FromResult(Enumerable.Empty<Project>());
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting deleted projects");

      return Task.FromResult(Enumerable.Empty<Project>());
    }
  }

  public async Task<IEnumerable<Project>> Handle(SearchProjectsQuery request, CancellationToken cancellationToken) {
    try {
      var query = _context.Projects
                          .Where(p => p.DeletedAt == null);

      // Search term
      if (!string.IsNullOrEmpty(request.SearchTerm)) {
        query = query.Where(p =>
                              p.Title.Contains(request.SearchTerm) ||
                              (p.Description != null && p.Description.Contains(request.SearchTerm)) ||
                              (p.ShortDescription != null && p.ShortDescription.Contains(request.SearchTerm))
        );
      }

      // Apply filters
      if (request.Type.HasValue) query = query.Where(p => p.Type == request.Type.Value);

      if (request.CategoryId.HasValue) query = query.Where(p => p.CategoryId == request.CategoryId.Value);

      if (request.Status.HasValue) query = query.Where(p => p.Status == request.Status.Value);

      if (request.Visibility.HasValue) query = query.Where(p => p.Visibility == request.Visibility.Value);

      query = ApplyAccessControl(query);
      query = ApplySorting(query, request.SortBy, request.SortDirection);

      query = query.Include(p => p.CreatedBy)
                   .Include(p => p.Category)
                   .Skip(request.Skip)
                   .Take(request.Take);

      return await query.ToListAsync(cancellationToken);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error searching projects");

      return Enumerable.Empty<Project>();
    }
  }

  public async Task<ProjectStatistics> Handle(GetProjectStatisticsQuery request, CancellationToken cancellationToken) {
    try { return await GetProjectStatistics(request.ProjectId, cancellationToken, request.FromDate, request.ToDate); }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting project statistics: {ProjectId}", request.ProjectId);

      return new ProjectStatistics();
    }
  }

  public async Task<IEnumerable<Project>> Handle(GetPopularProjectsQuery request, CancellationToken cancellationToken) {
    try {
      var query = _context.Projects
                          .Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published);

      if (request.Type.HasValue) { query = query.Where(p => p.Type == request.Type.Value); }

      // TODO: Implement popularity scoring based on views, likes, downloads
      query = ApplyAccessControl(query);
      query = query.Include(p => p.CreatedBy)
                   .Include(p => p.Category)
                   .OrderByDescending(p => p.CreatedAt) // Temporary sorting
                   .Take(request.Take);

      return await query.ToListAsync(cancellationToken);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting popular projects");

      return Enumerable.Empty<Project>();
    }
  }

  public async Task<IEnumerable<Project>> Handle(GetRecentProjectsQuery request, CancellationToken cancellationToken) {
    try {
      var query = _context.Projects
                          .Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published);

      if (request.Type.HasValue) { query = query.Where(p => p.Type == request.Type.Value); }

      query = ApplyAccessControl(query);
      query = query.Include(p => p.CreatedBy)
                   .Include(p => p.Category)
                   .OrderByDescending(p => p.CreatedAt)
                   .Take(request.Take);

      return await query.ToListAsync(cancellationToken);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting recent projects");

      return Enumerable.Empty<Project>();
    }
  }

  public async Task<IEnumerable<Project>> Handle(GetFeaturedProjectsQuery request, CancellationToken cancellationToken) {
    try {
      var query = _context.Projects
                          .Where(p => p.DeletedAt == null && p.Status == ContentStatus.Published);

      if (request.Type.HasValue) { query = query.Where(p => p.Type == request.Type.Value); }

      // TODO: Add featured flag to Project model
      query = ApplyAccessControl(query);
      query = query.Include(p => p.CreatedBy)
                   .Include(p => p.Category)
                   .OrderByDescending(p => p.CreatedAt) // Temporary sorting
                   .Take(request.Take);

      return await query.ToListAsync(cancellationToken);
    }
    catch (Exception ex) {
      _logger.LogError(ex, "Error getting featured projects");

      return Enumerable.Empty<Project>();
    }
  }

  /// <summary>
  /// Apply access control based on user context
  /// </summary>
  private IQueryable<Project> ApplyAccessControl(IQueryable<Project> query) {
    // Public projects are always visible
    var accessibleQuery = query.Where(p => p.Visibility == AccessLevel.Public);

    if (_userContext.IsAuthenticated) {
      // Authenticated users can see their own private projects
      accessibleQuery = query.Where(p =>
                                      p.Visibility == AccessLevel.Public ||
                                      (p.Visibility == AccessLevel.Private && p.Collaborators.Any(c => c.UserId == _userContext.UserId))
      );

      // Admins can see everything
      if (_userContext.IsInRole("Admin")) { accessibleQuery = query; }
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
      _ => query.OrderByDescending(p => p.CreatedAt)
    };
  }

  /// <summary>
  /// Get project statistics
  /// </summary>
  private Task<ProjectStatistics> GetProjectStatistics(Guid projectId, CancellationToken cancellationToken, DateTime? fromDate = null, DateTime? toDate = null) {
    // TODO: Implement actual statistics calculation
    // This would typically involve querying related tables for views, downloads, likes, etc.
    return Task.FromResult(new ProjectStatistics {
      ProjectId = projectId,
      FollowerCount = 0,
      FeedbackCount = 0,
      TotalDownloads = 0,
      ActiveTeamCount = 0,
      CollaboratorCount = 0,
      ReleaseCount = 0,
      JamSubmissionCount = 0,
      AwardCount = 0
    });
  }
}
