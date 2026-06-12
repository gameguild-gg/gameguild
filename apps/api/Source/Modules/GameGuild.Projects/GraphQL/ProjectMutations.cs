using GameGuild.Identity.Context.Actors;
using HotChocolate;
using HotChocolate.Types;

namespace GameGuild.Projects;

/// <summary>
/// GraphQL mutations for Project module using CQRS pattern
/// </summary>
[ExtendObjectType("Mutation")]
public class ProjectMutations {
  /// <summary>
  /// Creates a new project using CQRS pattern
  /// </summary>
  public async Task<Project> CreateProject(CreateProjectInput input, [Service] CQRS.IMediator mediator, [Service] IActorContextAccessor actorContextAccessor, CancellationToken cancellationToken) {
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
      Visibility = input.Visibility ?? ContentVisibility.Public,
      Status = input.Status ?? ContentStatus.Draft,
      Tags = input.Tags,
    };

    var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
    if (result.IsSuccess) return result.Value;
    throw new GraphQLException(result.Error.Description);
  }

  /// <summary>
  /// Updates an existing project using CQRS pattern
  /// </summary>
  public async Task<Project> UpdateProject(UpdateProjectInput input, [Service] CQRS.IMediator mediator, [Service] IActorContextAccessor actorContextAccessor, CancellationToken cancellationToken) {
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

    var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
    if (result.IsSuccess) return result.Value;
    throw new GraphQLException(result.Error.Description);
  }

  /// <summary>
  /// Deletes a project using CQRS pattern
  /// </summary>
  public async Task<bool> DeleteProject(Guid projectId, [Service] CQRS.IMediator mediator, [Service] IActorContextAccessor actorContextAccessor, bool softDelete = true, string? reason = null, CancellationToken cancellationToken = default) {
    var actor = actorContextAccessor.ActorContext;
    var command = new DeleteProjectCommand { ProjectId = projectId, DeletedBy = actor.SubjectIdAsGuid ?? Guid.Empty, SoftDelete = softDelete, Reason = reason };

    var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
    if (result.IsSuccess) return result.Value;
    throw new GraphQLException(result.Error.Description);
  }

  /// <summary>
  /// Publishes a project using CQRS pattern
  /// </summary>
  public async Task<Project> PublishProject(Guid projectId, [Service] CQRS.IMediator mediator, [Service] IActorContextAccessor actorContextAccessor, CancellationToken cancellationToken) {
    var actor = actorContextAccessor.ActorContext;
    var command = new PublishProjectCommand { ProjectId = projectId, PublishedBy = actor.SubjectIdAsGuid ?? Guid.Empty };

    var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
    if (result.IsSuccess) return result.Value;
    throw new GraphQLException(result.Error.Description);
  }

  /// <summary>
  /// Unpublishes a project using CQRS pattern
  /// </summary>
  public async Task<Project> UnpublishProject(Guid projectId, [Service] CQRS.IMediator mediator, [Service] IActorContextAccessor actorContextAccessor, CancellationToken cancellationToken) {
    var actor = actorContextAccessor.ActorContext;
    var command = new UnpublishProjectCommand { ProjectId = projectId, UnpublishedBy = actor.SubjectIdAsGuid ?? Guid.Empty };

    var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
    if (result.IsSuccess) return result.Value;
    throw new GraphQLException(result.Error.Description);
  }

  /// <summary>
  /// Archives a project using CQRS pattern
  /// </summary>
  public async Task<Project> ArchiveProject(Guid projectId, [Service] CQRS.IMediator mediator, [Service] IActorContextAccessor actorContextAccessor, CancellationToken cancellationToken) {
    var actor = actorContextAccessor.ActorContext;
    var command = new ArchiveProjectCommand { ProjectId = projectId, ArchivedBy = actor.SubjectIdAsGuid ?? Guid.Empty };

    var result = await mediator.Send(command, cancellationToken).ConfigureAwait(false);
    if (result.IsSuccess) return result.Value;
    throw new GraphQLException(result.Error.Description);
  }
}
