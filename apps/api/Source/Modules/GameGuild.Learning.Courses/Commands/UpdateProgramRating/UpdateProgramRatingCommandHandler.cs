using GameGuild.Abstractions;
using GameGuild.CQRS;

using GameGuild.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Programs;

/// <summary>
/// Command handler for UpdateProgramRatingCommand
/// </summary>
public class UpdateProgramRatingCommandHandler(IApplicationDbContext context, ILogger<UpdateProgramRatingCommandHandler> logger)
    : ICommandHandler<UpdateProgramRatingCommand, ProgramRating>
{
    public async Task<ProgramRating> Handle(UpdateProgramRatingCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Updating rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    var rating = await context.Set<ProgramRating>().Where(pr => pr.ProgramId == request.ProgramId && pr.UserId == request.UserId).FirstOrDefaultAsync(cancellationToken);

    if (rating == null) { throw new InvalidOperationException("Rating not found"); }

    rating.Rating = request.Rating;
    rating.Review = request.Review;
    rating.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Updated rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    return rating;
  }
}
