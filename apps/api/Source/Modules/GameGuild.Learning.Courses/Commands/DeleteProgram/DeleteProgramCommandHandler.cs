using GameGuild.Abstractions;
using GameGuild.CQRS;

using GameGuild.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Programs;

/// <summary>
/// Command handler for DeleteProgramCommand
/// </summary>
public class DeleteProgramCommandHandler(IApplicationDbContext context, ILogger<DeleteProgramCommandHandler> logger)
    : ICommandHandler<DeleteProgramCommand, bool>
{
    public async Task<bool> Handle(DeleteProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Deleting program: {ProgramId}", request.Id);

    var program = await context.Set<Program>().Where(p => p.Id == request.Id && p.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);

    if (program == null) { return false; }

    // Soft delete
    program.DeletedAt = DateTime.UtcNow;
    program.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Deleted program: {ProgramId}", program.Id);

    return true;
  }
}
