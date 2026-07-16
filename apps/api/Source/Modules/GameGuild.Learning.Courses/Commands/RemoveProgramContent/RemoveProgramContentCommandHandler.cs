using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Command handler for RemoveProgramContentCommand
/// </summary>
public sealed class RemoveProgramContentCommandHandler(
    IApplicationDbContext context,
    IProgramContentScheduleGuard scheduleGuard,
    IProgramContentLifecycleGuard lifecycleGuard,
    ILogger<RemoveProgramContentCommandHandler> logger)
    : ICommandHandler<RemoveProgramContentCommand, bool>
{
    public async Task<bool> Handle(RemoveProgramContentCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Removing content {ContentId} from program {ProgramId}", request.ContentId, request.ProgramId);

    var contents = await context.Set<ProgramContent>()
      .Where(pc => pc.ProgramId == request.ProgramId && pc.DeletedAt == null)
      .ToArrayAsync(cancellationToken)
      .ConfigureAwait(false);
    var programContent = contents.FirstOrDefault(pc => pc.Id == request.ContentId);

    if (programContent == null) { return false; }

    var contentTreeIds = ProgramContentTree.GetIds(request.ContentId, contents);
    await using var lifecycleTransaction = await ProgramContentLifecycleDatabaseLock
      .AcquireAsync(context, contentTreeIds, cancellationToken)
      .ConfigureAwait(false);

    foreach (var contentId in contentTreeIds) {
      if (await scheduleGuard.HasActiveScheduleReference(contentId, cancellationToken).ConfigureAwait(false)) {
        throw new RequestValidationException(
          "Content used by an active class schedule cannot be deleted. Remove or replace its schedule entry first.");
      }
      if (await lifecycleGuard.HasBlockingDeleteReference(contentId, cancellationToken).ConfigureAwait(false)) {
        throw new RequestValidationException(
          "Content linked to an assessment cue cannot be deleted. Remove the assessment cue first.");
      }
    }

    context.Set<ProgramContent>().Remove(programContent);
    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    await ProgramContentLifecycleDatabaseLock.CommitAsync(lifecycleTransaction, cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Removed content {ContentId} from program {ProgramId}", request.ContentId, request.ProgramId);

    return true;
  }

}
