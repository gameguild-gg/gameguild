using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Command handler for UpdateProgramRatingCommand
/// </summary>
public sealed class UpdateProgramRatingCommandHandler(IApplicationDbContext context, ILogger<UpdateProgramRatingCommandHandler> logger)
    : ICommandHandler<UpdateProgramRatingCommand, ProgramRating>
{
    public async Task<ProgramRating> Handle(UpdateProgramRatingCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Updating rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    var rating = await context.Set<ProgramRating>().Where(pr => pr.ProgramId == request.ProgramId && pr.UserId == request.UserId).FirstOrDefaultAsync(cancellationToken);

    if (rating == null) { throw new InvalidOperationException("Rating not found"); }

    rating.Rating = request.Rating;
    rating.Review = request.Review;
    rating.Touch();

    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Updated rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    return rating;
  }
}
