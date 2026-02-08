using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

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
    program.SoftDelete();
    program.Touch();

    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Deleted program: {ProgramId}", program.Id);

    return true;
  }
}
