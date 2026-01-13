using GameGuild.Abstractions;
using GameGuild.CQRS;

using GameGuild.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Command handler for BulkArchiveProgramsCommand
/// </summary>
public class BulkArchiveProgramsCommandHandler(IApplicationDbContext context, ILogger<BulkArchiveProgramsCommandHandler> logger)
    : ICommandHandler<BulkArchiveProgramsCommand, IEnumerable<Program>>
{
    public async Task<IEnumerable<Program>> Handle(BulkArchiveProgramsCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Bulk archiving {Count} programs", request.ProgramIds.Count());

        var programs = await context.Set<Program>()
            .Where(p => request.ProgramIds.Contains(p.Id) && p.DeletedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var program in programs)
        {
            program.Status = ContentStatus.Archived;
            program.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Bulk archived {Count} programs", programs.Count);

        return programs;
    }
}
