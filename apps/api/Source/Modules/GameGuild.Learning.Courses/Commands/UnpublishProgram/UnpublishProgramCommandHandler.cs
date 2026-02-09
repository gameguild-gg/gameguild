using GameGuild.CQRS;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Command handler for UnpublishProgramCommand
/// </summary>
public sealed class UnpublishProgramCommandHandler(IApplicationDbContext context, ILogger<UnpublishProgramCommandHandler> logger)
    : ICommandHandler<UnpublishProgramCommand, Program>
{
    public async Task<Program> Handle(UnpublishProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Unpublishing program: {ProgramId}", request.Id);

    var program = await context.Set<Program>().Where(p => p.Id == request.Id && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.Id} not found"); }

    program.Status = ContentStatus.Draft;
    program.Visibility = ContentVisibility.Private;
    program.Touch();

    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Unpublished program: {ProgramId}", program.Id);

    return program;
  }
}
