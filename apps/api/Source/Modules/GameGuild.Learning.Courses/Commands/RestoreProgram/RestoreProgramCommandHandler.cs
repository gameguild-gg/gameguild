using GameGuild.CQRS;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Command handler for RestoreProgramCommand
/// </summary>
public class RestoreProgramCommandHandler(IApplicationDbContext context, ILogger<RestoreProgramCommandHandler> logger)
    : ICommandHandler<RestoreProgramCommand, Program>
{
    public async Task<Program> Handle(RestoreProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Restoring program: {ProgramId}", request.Id);

    var program = await context.Set<Program>().Where(p => p.Id == request.Id && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.Id} not found"); }

    program.Status = ContentStatus.Draft;
    program.Touch();

    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Restored program: {ProgramId}", program.Id);

    return program;
  }
}
