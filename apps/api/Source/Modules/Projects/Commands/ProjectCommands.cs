using GameGuild.Modules.Contents;
using MediatR;


namespace GameGuild.Modules.Projects;

/// <summary>
/// Command to create a new project
/// </summary>
public record CreateProjectCommand : IRequest<CreateProjectResult> {
  public string Title { get; init; } = string.Empty;

  public string? Description { get; init; }

  public string? ShortDescription { get; init; }

  public string? ImageUrl { get; init; }

  public string? RepositoryUrl { get; init; }

  public string? WebsiteUrl { get; init; }

  public string? DownloadUrl { get; init; }

  public GameGuild.Common.ProjectType Type { get; init; } = Common.ProjectType.Game;

  public Guid CreatedById { get; init; }

  public Guid? CategoryId { get; init; }

  public AccessLevel Visibility { get; init; } = AccessLevel.Public;

  public ContentStatus Status { get; init; } = ContentStatus.Draft;

  public List<string>? Tags { get; init; }

  public Guid? TenantId { get; init; }
}

/// <summary>
/// Command to update an existing project
/// </summary>
public record UpdateProjectCommand : IRequest<UpdateProjectResult> {
  public Guid ProjectId { get; init; }

  public string? Title { get; init; }

  public string? Description { get; init; }

  public string? ShortDescription { get; init; }

  public string? ImageUrl { get; init; }

  public string? RepositoryUrl { get; init; }

  public string? WebsiteUrl { get; init; }

  public string? DownloadUrl { get; init; }

  public GameGuild.Common.ProjectType? Type { get; init; }

  public Guid? CategoryId { get; init; }

  public AccessLevel? Visibility { get; init; }

  public ContentStatus? Status { get; init; }

  public List<string>? Tags { get; init; }

  public Guid UpdatedBy { get; init; }
}

/// <summary>
/// Command to delete a project
/// </summary>
public record DeleteProjectCommand : IRequest<DeleteProjectResult> {
  public Guid ProjectId { get; init; }

  public Guid DeletedBy { get; init; }

  public bool SoftDelete { get; init; } = true;

  public string? Reason { get; init; }
}

/// <summary>
/// Command to publish a project
/// </summary>
public record PublishProjectCommand : IRequest<PublishProjectResult> {
  public Guid ProjectId { get; init; }

  public Guid PublishedBy { get; init; }
}

/// <summary>
/// Command to unpublish a project
/// </summary>
public record UnpublishProjectCommand : IRequest<UnpublishProjectResult> {
  public Guid ProjectId { get; init; }

  public Guid UnpublishedBy { get; init; }
}

/// <summary>
/// Command to archive a project
/// </summary>
public record ArchiveProjectCommand : IRequest<ArchiveProjectResult> {
  public Guid ProjectId { get; init; }

  public Guid ArchivedBy { get; init; }
}

// Result types
public record CreateProjectResult {
  public bool Success { get; init; }

  public Project? Project { get; init; }

  public string? ErrorMessage { get; init; }
}

public record UpdateProjectResult {
  public bool Success { get; init; }

  public Project? Project { get; init; }

  public string? ErrorMessage { get; init; }
}

public record DeleteProjectResult {
  public bool Success { get; init; }

  public string? ErrorMessage { get; init; }
}

public record PublishProjectResult {
  public bool Success { get; init; }

  public Project? Project { get; init; }

  public string? ErrorMessage { get; init; }
}

public record UnpublishProjectResult {
  public bool Success { get; init; }

  public Project? Project { get; init; }

  public string? ErrorMessage { get; init; }
}

public record ArchiveProjectResult {
  public bool Success { get; init; }

  public Project? Project { get; init; }

  public string? ErrorMessage { get; init; }
}
