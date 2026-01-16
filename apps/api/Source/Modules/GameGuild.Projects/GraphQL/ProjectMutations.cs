using GameGuild.Identity.Context.Actors;
using GameGuild.Enums;
using AccessLevel = GameGuild.Enums.AccessLevel;

namespace GameGuild.Projects;

/// <summary>
/// GraphQL mutations for Project module using CQRS pattern
/// </summary>
// [ExtendObjectType<Mutation>] // TODO: Configure GraphQL Mutation type
public class ProjectMutations {
  /// <summary>
  /// Creates a new project using CQRS pattern
  /// </summary>
  public async Task<CreateProjectResult> CreateProject(CreateProjectInput input, [Service] CQRS.IMediator mediator, [Service] IActorContextAccessor actorContextAccessor, CancellationToken cancellationToken) {
    var actor = actorContextAccessor.ActorContext;
    var command = new CreateProjectCommand {
      Title = input.Title,
      Description = input.Description,
      ShortDescription = input.ShortDescription,
      ImageUrl = input.ImageUrl,
      RepositoryUrl = input.RepositoryUrl,
      WebsiteUrl = input.WebsiteUrl,
      DownloadUrl = input.DownloadUrl,
      Type = (GameGuild.ProjectType)input.Type,
      CreatedById = actor.SubjectIdAsGuid ?? Guid.Empty,
      CategoryId = input.CategoryId,
      Visibility = input.Visibility ?? AccessLevel.Public,
      Status = input.Status ?? ContentStatus.Draft,
      Tags = input.Tags,
    };

    return await mediator.Send(command, cancellationToken);
  }

  /// <summary>
  /// Updates an existing project using CQRS pattern
  /// </summary>
  public async Task<UpdateProjectResult> UpdateProject(UpdateProjectInput input, [Service] CQRS.IMediator mediator, [Service] IActorContextAccessor actorContextAccessor, CancellationToken cancellationToken) {
    var actor = actorContextAccessor.ActorContext;
    var command = new UpdateProjectCommand {
      ProjectId = input.ProjectId,
      Title = input.Title,
      Description = input.Description,
      ShortDescription = input.ShortDescription,
      ImageUrl = input.ImageUrl,
      RepositoryUrl = input.RepositoryUrl,
      WebsiteUrl = input.WebsiteUrl,
      DownloadUrl = input.DownloadUrl,
      Type = input.Type,
      CategoryId = input.CategoryId,
      Visibility = input.Visibility,
      Status = input.Status,
      Tags = input.Tags,
      UpdatedBy = actor.SubjectIdAsGuid ?? Guid.Empty,
    };

    return await mediator.Send(command, cancellationToken);
  }

  /// <summary>
  /// Deletes a project using CQRS pattern
  /// </summary>
  public async Task<DeleteProjectResult> DeleteProject(Guid projectId, [Service] CQRS.IMediator mediator, [Service] IActorContextAccessor actorContextAccessor, bool softDelete = true, string? reason = null, CancellationToken cancellationToken = default) {
    var actor = actorContextAccessor.ActorContext;
    var command = new DeleteProjectCommand { ProjectId = projectId, DeletedBy = actor.SubjectIdAsGuid ?? Guid.Empty, SoftDelete = softDelete, Reason = reason };

    return await mediator.Send(command, cancellationToken);
  }

  /// <summary>
  /// Publishes a project using CQRS pattern
  /// </summary>
  public async Task<PublishProjectResult> PublishProject(Guid projectId, [Service] CQRS.IMediator mediator, [Service] IActorContextAccessor actorContextAccessor, CancellationToken cancellationToken) {
    var actor = actorContextAccessor.ActorContext;
    var command = new PublishProjectCommand { ProjectId = projectId, PublishedBy = actor.SubjectIdAsGuid ?? Guid.Empty };

    return await mediator.Send(command, cancellationToken);
  }

  /// <summary>
  /// Unpublishes a project using CQRS pattern
  /// </summary>
  public async Task<UnpublishProjectResult> UnpublishProject(Guid projectId, [Service] CQRS.IMediator mediator, [Service] IActorContextAccessor actorContextAccessor, CancellationToken cancellationToken) {
    var actor = actorContextAccessor.ActorContext;
    var command = new UnpublishProjectCommand { ProjectId = projectId, UnpublishedBy = actor.SubjectIdAsGuid ?? Guid.Empty };

    return await mediator.Send(command, cancellationToken);
  }

  /// <summary>
  /// Archives a project using CQRS pattern
  /// </summary>
  public async Task<ArchiveProjectResult> ArchiveProject(Guid projectId, [Service] CQRS.IMediator mediator, [Service] IActorContextAccessor actorContextAccessor, CancellationToken cancellationToken) {
    var actor = actorContextAccessor.ActorContext;
    var command = new ArchiveProjectCommand { ProjectId = projectId, ArchivedBy = actor.SubjectIdAsGuid ?? Guid.Empty };

    return await mediator.Send(command, cancellationToken);
  }
}
