using GameGuild.Abstractions;
using GameGuild.CQRS;

using GameGuild.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Command handler for RemoveProgramContentCommand
/// </summary>
public class RemoveProgramContentCommandHandler(IApplicationDbContext context, ILogger<RemoveProgramContentCommandHandler> logger)
    : ICommandHandler<RemoveProgramContentCommand, bool>
{
    public async Task<bool> Handle(RemoveProgramContentCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Removing content {ContentId} from program {ProgramId}", request.ContentId, request.ProgramId);

    var programContent = await context.Set<ProgramContent>().Where(pc => pc.ProgramId == request.ProgramId && pc.Id == request.ContentId).FirstOrDefaultAsync(cancellationToken);

    if (programContent == null) { return false; }

    context.Set<ProgramContent>().Remove(programContent);
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Removed content {ContentId} from program {ProgramId}", request.ContentId, request.ProgramId);

    return true;
  }
}
