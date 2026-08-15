using GameGuild.CQRS;

namespace GameGuild.Projects;

/// <summary> Query to get all projects </summary>
public sealed record GetAllProjectsQuery : IQuery<Result<IEnumerable<Project>>> {
  public ProjectType? Type { get; init; }

  public ContentStatus? Status { get; init; }

  public ContentVisibility? Visibility { get; init; }

  public Guid? CreatorId { get; init; }

  public Guid? CategoryId { get; init; }

  public string? SearchTerm { get; init; }

  public List<string>? Tags { get; init; }

  /// <summary>
  /// Filter to featured projects only. True = featured only, False = non-featured only, null = all.
  /// Replaces legacy /v1/projects/featured endpoint (Google API Guidelines compliance).
  /// </summary>
  public bool? Featured { get; init; }

  /// <summary>
  /// Filter to popular projects. True = popular only (sorted by popularity), null = no filter.
  /// Replaces legacy /v1/projects/popular endpoint (Google API Guidelines compliance).
  /// </summary>
  public bool? Popular { get; init; }

  /// <summary>
  /// Filter to recently created/updated projects. True = recent only (sorted by date), null = no filter.
  /// Replaces legacy /v1/projects/recent endpoint (Google API Guidelines compliance).
  /// </summary>
  public bool? Recent { get; init; }

  public int Skip { get; init; } = 0;

  public int Take { get; init; } = 50;

  public string? SortBy { get; init; } = "CreatedAt";

  public string? SortDirection { get; init; } = "DESC";

  public bool IncludeDeleted { get; init; } = false;

  /// <summary>
  /// Includes archived lifecycle records. This is an administrative list filter;
  /// callers without Project administration have the flag ignored by the API.
  /// </summary>
  public bool IncludeArchived { get; init; } = false;

  /// <summary>
  /// Restricts results to the actor's active tenant even when the actor is a system administrator.
  /// Intended for tenant-owned workflows such as Testing Lab project selection.
  /// </summary>
  public bool CurrentTenantOnly { get; init; } = false;
}

/// <summary> Query to get project by ID </summary>
public sealed record GetProjectByIdQuery : IQuery<Result<Project?>> {
  public Guid ProjectId { get; init; }

  public bool IncludeTeam { get; init; } = true;

  public bool IncludeReleases { get; init; } = true;

  public bool IncludeCollaborators { get; init; } = true;

  public bool IncludeStatistics { get; init; } = false;
}

/// <summary> Query to get project by slug </summary>
public sealed record GetProjectBySlugQuery : IQuery<Result<Project?>> {
  public string Slug { get; init; } = string.Empty;

  public bool IncludeTeam { get; init; } = true;

  public bool IncludeReleases { get; init; } = true;

  public bool IncludeCollaborators { get; init; } = true;
}

/// <summary> Query to get projects by category </summary>
public sealed record GetProjectsByCategoryQuery : IQuery<Result<IEnumerable<Project>>> {
  public Guid CategoryId { get; init; }

  public ContentStatus? Status { get; init; }

  public int Skip { get; init; } = 0;

  public int Take { get; init; } = 50;
}

/// <summary> Query to get projects by creator </summary>
public sealed record GetProjectsByCreatorQuery : IQuery<Result<IEnumerable<Project>>> {
  public Guid CreatorId { get; init; }

  public ContentStatus? Status { get; init; }

  public int Skip { get; init; } = 0;

  public int Take { get; init; } = 50;
}

/// <summary> Query to get projects by status </summary>
public sealed record GetProjectsByStatusQuery : IQuery<Result<IEnumerable<Project>>> {
  public ContentStatus Status { get; init; }

  public ProjectType? Type { get; init; }

  public int Skip { get; init; } = 0;

  public int Take { get; init; } = 50;
}

/// <summary> Query to get deleted projects (admin only) </summary>
public sealed record GetDeletedProjectsQuery : IQuery<Result<IEnumerable<Project>>> {
  public int Skip { get; init; } = 0;

  public int Take { get; init; } = 50;
}

/// <summary> Query to search projects </summary>
public sealed record SearchProjectsQuery : IQuery<Result<IEnumerable<Project>>> {
  public string SearchTerm { get; init; } = string.Empty;

  public ProjectType? Type { get; init; }

  public Guid? CategoryId { get; init; }

  public List<string>? Tags { get; init; }

  public ContentStatus? Status { get; init; }

  public ContentVisibility? Visibility { get; init; }

  public int Skip { get; init; } = 0;

  public int Take { get; init; } = 50;

  public string? SortBy { get; init; } = "Relevance";

  public string? SortDirection { get; init; } = "DESC";
}

/// <summary> Query to get project statistics </summary>
public sealed record GetProjectStatisticsQuery : IQuery<Result<ProjectStatistics>> {
  public Guid ProjectId { get; init; }

  public DateTime? FromDate { get; init; }

  public DateTime? ToDate { get; init; }
}

/// <summary> Query to get popular projects </summary>
public sealed record GetPopularProjectsQuery : IQuery<Result<IEnumerable<Project>>> {
  public ProjectType? Type { get; init; }

  public TimeSpan? TimeWindow { get; init; } = TimeSpan.FromDays(30);

  public int Take { get; init; } = 10;
}

/// <summary> Query to get recent projects </summary>
public sealed record GetRecentProjectsQuery : IQuery<Result<IEnumerable<Project>>> {
  public ProjectType? Type { get; init; }

  public int Take { get; init; } = 10;
}

/// <summary> Query to get featured projects </summary>
public sealed record GetFeaturedProjectsQuery : IQuery<Result<IEnumerable<Project>>> {
  public ProjectType? Type { get; init; }

  public int Take { get; init; } = 10;
}
