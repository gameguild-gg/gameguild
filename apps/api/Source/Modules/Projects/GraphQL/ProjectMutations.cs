using GameGuild.Common;
using GameGuild.Modules.Contents;
using MediatR;

namespace GameGuild.Modules.Projects;

/// <summary>
/// GraphQL mutations for Project module using CQRS pattern
/// </summary>
[ExtendObjectType<Mutation>]
public class ProjectMutations
{
    /// <summary>
    /// Creates a new project using CQRS pattern
    /// </summary>
    public async Task<CreateProjectResult> CreateProject(
        CreateProjectInput input,
        [Service] IMediator mediator,
        [Service] IUserContext userContext,
        CancellationToken cancellationToken)
    {
        var command = new CreateProjectCommand
        {
            Title = input.Title,
            Description = input.Description,
            ShortDescription = input.ShortDescription,
            ImageUrl = input.ImageUrl,
            RepositoryUrl = input.RepositoryUrl,
            WebsiteUrl = input.WebsiteUrl,
            DownloadUrl = input.DownloadUrl,
            Type = input.Type,
            CreatedById = userContext.UserId ?? Guid.Empty,
            CategoryId = input.CategoryId,
            Visibility = input.Visibility ?? AccessLevel.Public,
            Status = input.Status ?? ContentStatus.Draft,
            Tags = input.Tags
        };

        return await mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Updates an existing project using CQRS pattern
    /// </summary>
    public async Task<UpdateProjectResult> UpdateProject(
        UpdateProjectInput input,
        [Service] IMediator mediator,
        [Service] IUserContext userContext,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProjectCommand
        {
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
            UpdatedBy = userContext.UserId ?? Guid.Empty
        };

        return await mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Deletes a project using CQRS pattern
    /// </summary>
    public async Task<DeleteProjectResult> DeleteProject(
        Guid projectId,
        [Service] IMediator mediator,
        [Service] IUserContext userContext,
        bool softDelete = true,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteProjectCommand
        {
            ProjectId = projectId,
            DeletedBy = userContext.UserId ?? Guid.Empty,
            SoftDelete = softDelete,
            Reason = reason
        };

        return await mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Publishes a project using CQRS pattern
    /// </summary>
    public async Task<PublishProjectResult> PublishProject(
        Guid projectId,
        [Service] IMediator mediator,
        [Service] IUserContext userContext,
        CancellationToken cancellationToken)
    {
        var command = new PublishProjectCommand
        {
            ProjectId = projectId,
            PublishedBy = userContext.UserId ?? Guid.Empty
        };

        return await mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Unpublishes a project using CQRS pattern
    /// </summary>
    public async Task<UnpublishProjectResult> UnpublishProject(
        Guid projectId,
        [Service] IMediator mediator,
        [Service] IUserContext userContext,
        CancellationToken cancellationToken)
    {
        var command = new UnpublishProjectCommand
        {
            ProjectId = projectId,
            UnpublishedBy = userContext.UserId ?? Guid.Empty
        };

        return await mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Archives a project using CQRS pattern
    /// </summary>
    public async Task<ArchiveProjectResult> ArchiveProject(
        Guid projectId,
        [Service] IMediator mediator,
        [Service] IUserContext userContext,
        CancellationToken cancellationToken)
    {
        var command = new ArchiveProjectCommand
        {
            ProjectId = projectId,
            ArchivedBy = userContext.UserId ?? Guid.Empty
        };

        return await mediator.Send(command, cancellationToken);
    }
}
