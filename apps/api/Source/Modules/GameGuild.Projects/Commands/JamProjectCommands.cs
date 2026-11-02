using GameGuild.CQRS;
using GameGuild.GameJams;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Projects;

/// <summary>
/// Command factory for the project-management MVP (issue #84): a game jam materializes
/// the caller's workspace project automatically. The command reuses
/// <see cref="CreateProjectCommand"/> so the jam entry project goes through the standard
/// creation path (actor identity, tenant membership, quota, slug uniqueness, personal
/// owner team, creator collaborator) and then links the project to the jam with a
/// <see cref="ProjectJamSubmission"/> row. The link makes the factory idempotent: one
/// jam project per (jam, creator).
/// </summary>
public sealed record CreateJamProjectCommand(Guid JamId, string? Title = null)
    : ICommand<Result<Project>>;

public sealed class CreateJamProjectCommandHandler(
    IApplicationDbContext context,
    IActorContextAccessor actorContextAccessor,
    ISender sender,
    ILogger<CreateJamProjectCommandHandler> logger)
    : ICommandHandler<CreateJamProjectCommand, Result<Project>>
{
    private const string JamSubmissionNote = "Auto-created jam workspace";

    public async Task<Result<Project>> Handle(CreateJamProjectCommand request, CancellationToken cancellationToken)
    {
        // Acting user identity always comes from the authenticated actor context,
        // never from the request body (repository authorization invariant #8).
        var actor = actorContextAccessor.ActorContext;
        var userId = actor.SubjectIdAsGuid;
        if (!actor.IsAuthenticated || userId is null)
        {
            return Result.Failure<Project>(Error.Unauthorized("Project.Unauthenticated", "User must be authenticated"));
        }

        var jam = await context.Set<Jam>().AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == request.JamId, cancellationToken)
            .ConfigureAwait(false);
        if (jam is null)
        {
            return Result.Failure<Project>(Error.NotFound("Project.JamNotFound", "Jam not found"));
        }

        var existingProject = await FindExistingJamProjectAsync(request.JamId, userId.Value, cancellationToken).ConfigureAwait(false);
        if (existingProject is not null)
        {
            logger.LogInformation("Reusing jam project {ProjectId} for jam {JamId}", existingProject.Id, jam.Id);
            return Result.Success(existingProject);
        }

        var title = string.IsNullOrWhiteSpace(request.Title)
            ? $"{jam.Name} - jam entry"
            : request.Title.Trim();
        var creation = await sender.Send(new CreateProjectCommand
        {
            Title = title,
            ShortDescription = $"Workspace for the {jam.Name} game jam.",
            Description = jam.Description ?? jam.Theme,
            Type = GameGuild.ProjectType.Game,
            Visibility = ContentVisibility.Private,
            Status = ContentStatus.Draft,
            Tags = ["jam", jam.Slug],
            CreatedById = userId.Value
        }, cancellationToken).ConfigureAwait(false);
        if (creation.IsFailure)
        {
            return Result.Failure<Project>(creation.Error);
        }

        var project = creation.Value;
        context.Set<ProjectJamSubmission>().Add(new ProjectJamSubmission
        {
            ProjectId = project.Id,
            JamId = jam.Id,
            SubmittedAt = SystemClock.UtcNow,
            SubmissionNotes = JamSubmissionNote,
            IsEligible = true
        });
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Created jam project {ProjectId} for jam {JamId}", project.Id, jam.Id);
        return Result.Success(project);
    }

    private Task<Project?> FindExistingJamProjectAsync(Guid jamId, Guid userId, CancellationToken cancellationToken) =>
        (from submission in context.Set<ProjectJamSubmission>().AsNoTracking()
         join project in context.Set<Project>().AsNoTracking()
             on submission.ProjectId equals project.Id
         where submission.JamId == jamId
               && project.CreatedById == userId
               && project.DeletedAt == null
         select project)
        .FirstOrDefaultAsync(cancellationToken);
}
