using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Command handler for BulkUpdateProgramVisibilityCommand
/// </summary>
public class BulkUpdateProgramVisibilityCommandHandler(IApplicationDbContext context, ILogger<BulkUpdateProgramVisibilityCommandHandler> logger)
    : ICommandHandler<BulkUpdateProgramVisibilityCommand, IEnumerable<Program>>
{
    public async Task<IEnumerable<Program>> Handle(BulkUpdateProgramVisibilityCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Bulk updating visibility for {Count} programs", request.ProgramIds.Count());

        var programs = await context.Set<Program>()
            .Where(p => request.ProgramIds.Contains(p.Id) && p.DeletedAt == null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var program in programs)
        {
            program.Visibility = request.Visibility;
            program.Touch();
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Bulk updated visibility for {Count} programs", programs.Count);

        return programs;
    }
}
