using GameGuild.Modules.Contents;
using MediatR;


namespace GameGuild.Modules.Projects;

/// <summary>
/// Query to get all projects
/// </summary>
public record GetAllProjectsQuery : IRequest<IEnumerable<Project>> {
  public Common.ProjectType? Type { get; init; }

  public ContentStatus? Status { get; init; }

  public AccessLevel? Visibility { get; init; }

  public Guid? CreatorId { get; init; }

  public Guid? CategoryId { get; init; }

  public string? SearchTerm { get; init; }

  public List<string>? Tags { get; init; }

  public int Skip { get; init; } = 0;

  public int Take { get; init; } = 50;

  public string? SortBy { get; init; } = "CreatedAt";

  public string? SortDirection { get; init; } = "DESC";

  public bool IncludeDeleted { get; init; } = false;
}

/// <summary>
/// Query to get project by ID
/// </summary>
public record GetProjectByIdQuery : IRequest<Project?> {
  public Guid ProjectId { get; init; }

  public bool IncludeTeam { get; init; } = true;

  public bool IncludeReleases { get; init; } = true;

  public bool IncludeCollaborators { get; init; } = true;

  // New: include project versions collection
  public bool IncludeVersions { get; init; } = true;

  public bool IncludeStatistics { get; init; } = false;
}

/// <summary>
/// Query to get project by slug
/// </summary>
public record GetProjectBySlugQuery : IRequest<Project?> {
  public string Slug { get; init; } = string.Empty;

  public bool IncludeTeam { get; init; } = true;

  public bool IncludeReleases { get; init; } = true;

  public bool IncludeCollaborators { get; init; } = true;

  // New: include project versions collection
  public bool IncludeVersions { get; init; } = true;
}

/// <summary>
/// Query to get projects by category
/// </summary>
public record GetProjectsByCategoryQuery : IRequest<IEnumerable<Project>> {
  public Guid CategoryId { get; init; }

  public ContentStatus? Status { get; init; }

  public int Skip { get; init; } = 0;

  public int Take { get; init; } = 50;
}

/// <summary>
/// Query to get projects by creator
/// </summary>
public record GetProjectsByCreatorQuery : IRequest<IEnumerable<Project>> {
  public Guid CreatorId { get; init; }

  public ContentStatus? Status { get; init; }

  public int Skip { get; init; } = 0;

  public int Take { get; init; } = 50;
}

/// <summary>
/// Query to get projects by status
/// </summary>
public record GetProjectsByStatusQuery : IRequest<IEnumerable<Project>> {
  public ContentStatus Status { get; init; }

  public Common.ProjectType? Type { get; init; }

  public int Skip { get; init; } = 0;

  public int Take { get; init; } = 50;
}

/// <summary>
/// Query to get deleted projects (admin only)
/// </summary>
public record GetDeletedProjectsQuery : IRequest<IEnumerable<Project>> {
  public int Skip { get; init; } = 0;

  public int Take { get; init; } = 50;
}

/// <summary>
/// Query to search projects
/// </summary>
public record SearchProjectsQuery : IRequest<IEnumerable<Project>> {
  public string SearchTerm { get; init; } = string.Empty;

  public Common.ProjectType? Type { get; init; }

  public Guid? CategoryId { get; init; }

  public List<string>? Tags { get; init; }

  public ContentStatus? Status { get; init; }

  public AccessLevel? Visibility { get; init; }

  public int Skip { get; init; } = 0;

  public int Take { get; init; } = 50;

  public string? SortBy { get; init; } = "Relevance";

  public string? SortDirection { get; init; } = "DESC";
}

/// <summary>
/// Query to get project statistics
/// </summary>
public record GetProjectStatisticsQuery : IRequest<ProjectStatistics> {
  public Guid ProjectId { get; init; }

  public DateTime? FromDate { get; init; }

  public DateTime? ToDate { get; init; }
}

/// <summary>
/// Query to get popular projects
/// </summary>
public record GetPopularProjectsQuery : IRequest<IEnumerable<Project>> {
  public Common.ProjectType? Type { get; init; }

  public TimeSpan? TimeWindow { get; init; } = TimeSpan.FromDays(30);

  public int Take { get; init; } = 10;
}

/// <summary>
/// Query to get recent projects
/// </summary>
public record GetRecentProjectsQuery : IRequest<IEnumerable<Project>> {
  public Common.ProjectType? Type { get; init; }

  public int Take { get; init; } = 10;
}

/// <summary>
/// Query to get featured projects
/// </summary>
public record GetFeaturedProjectsQuery : IRequest<IEnumerable<Project>> {
  public Common.ProjectType? Type { get; init; }

  public int Take { get; init; } = 10;
}
