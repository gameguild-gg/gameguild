using GameGuild.Abstractions;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Command handler for AddToWishlistCommand
/// </summary>
public class AddToWishlistCommandHandler(IApplicationDbContext context, ILogger<AddToWishlistCommandHandler> logger)
    : ICommandHandler<AddToWishlistCommand, ProgramWishlist>
{
    public async Task<ProgramWishlist> Handle(AddToWishlistCommand request, CancellationToken cancellationToken) {
    logger.LogInformation("Adding program {ProgramId} to wishlist for user {UserId}", request.ProgramId, request.UserId);

    var existingWishlist = await context.Set<ProgramWishlist>().Where(pw => pw.ProgramId == request.ProgramId && pw.UserId == Guid.Parse(request.UserId)).FirstOrDefaultAsync(cancellationToken);

    if (existingWishlist != null) { throw new InvalidOperationException("Program is already in user's wishlist"); }

    var wishlist = new ProgramWishlist { ProgramId = request.ProgramId, UserId = Guid.Parse(request.UserId), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

    context.Set<ProgramWishlist>().Add(wishlist);
    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Added program {ProgramId} to wishlist for user {UserId}", request.ProgramId, request.UserId);

    return wishlist;
  }
}
