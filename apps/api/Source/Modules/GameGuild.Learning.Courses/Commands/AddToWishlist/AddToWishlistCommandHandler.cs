using GameGuild.CQRS;
using GameGuild.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Command handler for AddToWishlistCommand
/// </summary>
public sealed class AddToWishlistCommandHandler(IApplicationDbContext context, ILogger<AddToWishlistCommandHandler> logger, IPublisher? publisher = null)
    : ICommandHandler<AddToWishlistCommand, ProgramWishlist>
{
    public async Task<ProgramWishlist> Handle(AddToWishlistCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Adding program {ProgramId} to wishlist for user {UserId}", request.ProgramId, request.UserId);

    var userId = Guid.Parse(request.UserId);
    var existingWishlist = await context.Set<ProgramWishlist>().Where(pw => pw.ProgramId == request.ProgramId && pw.UserId == userId).FirstOrDefaultAsync(cancellationToken);

    if (existingWishlist != null) { throw new InvalidOperationException("Program is already in user's wishlist"); }

    var wishlist = new ProgramWishlist { ProgramId = request.ProgramId, UserId = userId };

    context.Set<ProgramWishlist>().Add(wishlist);
    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation("Added program {ProgramId} to wishlist for user {UserId}", request.ProgramId, request.UserId);

    await PublishWishlistedAsync(userId, request.ProgramId, cancellationToken).ConfigureAwait(false);

    return wishlist;
  }

    /// <summary>
    ///     Issue #1184 G4: CourseWishlistedEvent was declared but never raised; adding to the
    ///     wishlist is its producing site. Separate method so the publish stays outside the
    ///     persistence method (GGARCH003).
    /// </summary>
    private async Task PublishWishlistedAsync(Guid userId, Guid programId, CancellationToken cancellationToken)
    {
        if (publisher is null)
        {
            return;
        }

        await publisher.Publish(new CourseWishlistedEvent(userId, programId, null), cancellationToken).ConfigureAwait(false);
    }
}
