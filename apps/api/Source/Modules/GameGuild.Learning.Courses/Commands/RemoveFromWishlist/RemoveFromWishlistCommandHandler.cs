using GameGuild.Abstractions;
using GameGuild.CQRS;

using GameGuild.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Programs;

/// <summary>
/// Command handler for RemoveFromWishlistCommand
/// </summary>
public class RemoveFromWishlistCommandHandler(IApplicationDbContext context, ILogger<RemoveFromWishlistCommandHandler> logger)
    : ICommandHandler<RemoveFromWishlistCommand, bool>
{
    public async Task<bool> Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Removing program {ProgramId} from wishlist for user {UserId}", request.ProgramId, request.UserId);

    var wishlist = await context.Set<ProgramWishlist>().Where(pw => pw.ProgramId == request.ProgramId && pw.UserId == Guid.Parse(request.UserId)).FirstOrDefaultAsync(cancellationToken);

    if (wishlist == null) { return false; }

    context.Set<ProgramWishlist>().Remove(wishlist);
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Removed program {ProgramId} from wishlist for user {UserId}", request.ProgramId, request.UserId);

    return true;
  }
}
