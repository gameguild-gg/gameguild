using GameGuild.Abstractions;
using GameGuild.CQRS;

using GameGuild.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Programs;

/// <summary>
/// Command handler for DeleteProgramRatingCommand
/// </summary>
public class DeleteProgramRatingCommandHandler(IApplicationDbContext context, ILogger<DeleteProgramRatingCommandHandler> logger)
    : ICommandHandler<DeleteProgramRatingCommand, bool>
{
    public async Task<bool> Handle(DeleteProgramRatingCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Deleting rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    var rating = await context.Set<ProgramRating>().Where(pr => pr.ProgramId == request.ProgramId && pr.UserId == request.UserId).FirstOrDefaultAsync(cancellationToken);

    if (rating == null) { return false; }

    context.Set<ProgramRating>().Remove(rating);
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Deleted rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    return true;
  }
}
