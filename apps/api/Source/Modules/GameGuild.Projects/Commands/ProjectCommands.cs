using GameGuild.CQRS;
using GameGuild.Resources;

namespace GameGuild.Projects;

/// <summary>
/// Command to create a new project
/// </summary>
[RequiresQuota(ResourceUsageType.Projects, Source = "CreateProject")]
public sealed record CreateProjectCommand : ICommand<Result<Project>> {
  public string Title { get; init; } = string.Empty;

  public string? Description { get; init; }

  public string? ShortDescription { get; init; }

  public string? ImageUrl { get; init; }

  public string? RepositoryUrl { get; init; }

  public string? WebsiteUrl { get; init; }

  public string? DownloadUrl { get; init; }

  public GameGuild.ProjectType Type { get; init; } = GameGuild.ProjectType.Game;

  public Guid CreatedById { get; init; }

  public Guid? CategoryId { get; init; }

  public ContentVisibility Visibility { get; init; } = ContentVisibility.Public;

  public ContentStatus Status { get; init; } = ContentStatus.Draft;

  public List<string>? Tags { get; init; }

  public Guid? TenantId { get; init; }

  public Guid? OwnerTeamId { get; init; }
}

/// <summary>
/// Command to update an existing project
/// </summary>
public sealed record UpdateProjectCommand : ICommand<Result<Project>> {
  public Guid ProjectId { get; init; }

  public string? Title { get; init; }

  public string? Description { get; init; }

  public string? ShortDescription { get; init; }

  public string? ImageUrl { get; init; }

  public string? RepositoryUrl { get; init; }

  public string? WebsiteUrl { get; init; }

  public string? DownloadUrl { get; init; }

  public GameGuild.ProjectType? Type { get; init; }

  public Guid? CategoryId { get; init; }

  public ContentVisibility? Visibility { get; init; }

  public ContentStatus? Status { get; init; }

  public List<string>? Tags { get; init; }

  public Guid UpdatedBy { get; init; }
}

/// <summary>
/// Command to delete a project
/// </summary>
public sealed record DeleteProjectCommand : ICommand<Result<bool>> {
  public Guid ProjectId { get; init; }

  public Guid DeletedBy { get; init; }

  public bool SoftDelete { get; init; } = true;

  public string? Reason { get; init; }
}

/// <summary>
/// Command to publish a project
/// </summary>
public sealed record PublishProjectCommand : ICommand<Result<Project>> {
  public Guid ProjectId { get; init; }

  public Guid PublishedBy { get; init; }
}

/// <summary>
/// Command to unpublish a project
/// </summary>
public sealed record UnpublishProjectCommand : ICommand<Result<Project>> {
  public Guid ProjectId { get; init; }

  public Guid UnpublishedBy { get; init; }
}

/// <summary>
/// Command to archive a project
/// </summary>
public sealed record ArchiveProjectCommand : ICommand<Result<Project>> {
  public Guid ProjectId { get; init; }

  public Guid ArchivedBy { get; init; }
}
