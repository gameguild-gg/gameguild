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

    foreach (var contentId in ProgramContentTree.GetIds(request.ContentId, contents)) {
      if (await scheduleGuard.HasActiveScheduleReference(contentId, cancellationToken).ConfigureAwait(false)) {
        throw new RequestValidationException(
          "Content used by an active class schedule cannot be deleted. Remove or replace its schedule entry first.");
      }
    }

    context.Set<ProgramContent>().Remove(programContent);
    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Removed content {ContentId} from program {ProgramId}", request.ContentId, request.ProgramId);

    return true;
  }

}
