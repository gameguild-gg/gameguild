using GameGuild.CQRS;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Command handler for ReorderProgramContentCommand
/// </summary>
public sealed class ReorderProgramContentCommandHandler(IApplicationDbContext context, ILogger<ReorderProgramContentCommandHandler> logger)
    : ICommandHandler<ReorderProgramContentCommand, IEnumerable<ProgramContent>>
{
    public async Task<IEnumerable<ProgramContent>> Handle(ReorderProgramContentCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reordering content for program {ProgramId}", request.ProgramId);

        var programContents = await context.Set<ProgramContent>()
            .Where(pc => pc.ProgramId == request.ProgramId && request.ContentOrders.Keys.Contains(pc.Id))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var programContent in programContents)
        {
            if (request.ContentOrders.TryGetValue(programContent.Id, out var newOrder))
            {
                programContent.SortOrder = newOrder;
                programContent.Touch();
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Reordered content for program {ProgramId}", request.ProgramId);

        return programContents;
    }
}
