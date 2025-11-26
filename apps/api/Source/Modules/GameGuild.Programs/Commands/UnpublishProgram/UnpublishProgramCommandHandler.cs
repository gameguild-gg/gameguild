using GameGuild.Abstractions;
using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Programs.Commands;

/// <summary>
/// Command handler for UnpublishProgramCommand
/// </summary>
public class UnpublishProgramCommandHandler(IApplicationDbContext context, ILogger<UnpublishProgramCommandHandler> logger)
    : ICommandHandler<UnpublishProgramCommand, Program>
{
    public async Task<Program> Handle(UnpublishProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Unpublishing program: {ProgramId}", request.Id);

    var program = await context.Set<Program>().Where(p => p.Id == request.Id && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.Id} not found"); }

    program.Status = ContentStatus.Draft;
    program.Visibility = AccessLevel.Private;
    program.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Unpublished program: {ProgramId}", program.Id);

    return program;
  }
}
