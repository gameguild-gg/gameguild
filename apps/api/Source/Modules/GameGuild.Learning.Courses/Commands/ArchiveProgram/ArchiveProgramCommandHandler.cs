using GameGuild.Abstractions;
using GameGuild.CQRS;

using GameGuild.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Command handler for ArchiveProgramCommand
/// </summary>
public class ArchiveProgramCommandHandler(IApplicationDbContext context, ILogger<ArchiveProgramCommandHandler> logger)
    : ICommandHandler<ArchiveProgramCommand, Program>
{
    public async Task<Program> Handle(ArchiveProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Archiving program: {ProgramId}", request.Id);

    var program = await context.Set<Program>().Where(p => p.Id == request.Id && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { throw new InvalidOperationException($"Program with ID {request.Id} not found"); }

    program.Status = ContentStatus.Archived;
    program.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Archived program: {ProgramId}", program.Id);

    return program;
  }
}
