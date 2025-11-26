using GameGuild.Abstractions;
using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Programs.Commands;

/// <summary>
/// Command handler for ReorderProgramContentCommand
/// </summary>
public class ReorderProgramContentCommandHandler(IApplicationDbContext context, ILogger<ReorderProgramContentCommandHandler> logger)
    : ICommandHandler<ReorderProgramContentCommand, IEnumerable<ProgramContent>>
{
    public async Task<IEnumerable<ProgramContent>> Handle(ReorderProgramContentCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reordering content for program {ProgramId}", request.ProgramId);

        var programContents = await context.Set<ProgramContent>()
            .Where(pc => pc.ProgramId == request.ProgramId && request.ContentOrders.Keys.Contains(pc.Id))
            .ToListAsync(cancellationToken);

        foreach (var programContent in programContents)
        {
            if (request.ContentOrders.TryGetValue(programContent.Id, out var newOrder))
            {
                programContent.SortOrder = newOrder;
                programContent.UpdatedAt = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Reordered content for program {ProgramId}", request.ProgramId);

        return programContents;
    }
}
