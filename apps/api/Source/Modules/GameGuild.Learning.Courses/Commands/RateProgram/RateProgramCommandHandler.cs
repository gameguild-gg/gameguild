using GameGuild.CQRS;
using GameGuild.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Command handler for RateProgramCommand
/// </summary>
public sealed class RateProgramCommandHandler(IApplicationDbContext context, ILogger<RateProgramCommandHandler> logger, IPublisher? publisher = null)
    : ICommandHandler<RateProgramCommand, ProgramRating>
{
    public async Task<ProgramRating> Handle(RateProgramCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Adding rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    var existingRating = await context.Set<ProgramRating>().Where(pr => pr.ProgramId == request.ProgramId && pr.UserId == request.UserId).FirstOrDefaultAsync(cancellationToken);

    if (existingRating != null) { throw new InvalidOperationException("User has already rated this program"); }

    ProgramUser? enrollment = null;
    if (Guid.TryParse(request.UserId, out var userGuid))
    {
        enrollment = await context.Set<ProgramUser>()
            .FirstOrDefaultAsync(
                pu => pu.ProgramId == request.ProgramId && pu.UserId == userGuid,
                cancellationToken)
            .ConfigureAwait(false);
    }

    var rating = new ProgramRating
    {
        ProgramId = request.ProgramId,
        UserId = request.UserId,
        ProgramUserId = enrollment?.Id,
        Rating = request.Rating,
        Review = request.Review,
        IsVerified = enrollment?.CompletedAt is not null
    };

    context.Set<ProgramRating>().Add(rating);
    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Added rating for program {ProgramId} by user {UserId}", request.ProgramId, request.UserId);

    await PublishRatedAsync(userGuid, request, cancellationToken).ConfigureAwait(false);

    return rating;
  }

    /// <summary>
    ///     Issue #1184 G4: CourseRatedEvent was declared but never raised; program ratings are
    ///     its producing site (first-time ratings only — re-ratings are rejected above).
    ///     Separate method so the publish stays outside the persistence method (GGARCH003).
    /// </summary>
    private async Task PublishRatedAsync(Guid userGuid, RateProgramCommand request, CancellationToken cancellationToken)
    {
        if (publisher is null || userGuid == Guid.Empty)
        {
            return;
        }

        await publisher.Publish(new CourseRatedEvent(userGuid, request.ProgramId, null, request.Rating, request.Review), cancellationToken).ConfigureAwait(false);
    }
}
