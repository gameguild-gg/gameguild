using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Experience.Social.Services;

/// <summary>
/// Service implementation for course wishlist (bookmark) operations
/// </summary>
public class WishlistService : IWishlistService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<WishlistService> _logger;

    public WishlistService(
        IApplicationDbContext context,
        ILogger<WishlistService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<CourseWishlist>> AddToWishlistAsync(
        Guid courseId,
        Guid userId,
        bool notifyOnSale = true,
        bool notifyOnUpdate = false,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.Set<CourseWishlist>()
            .FirstOrDefaultAsync(w => w.CourseId == courseId && w.UserId == userId, cancellationToken).ConfigureAwait(false);

        if (existing != null)
        {
            return Result.Failure<CourseWishlist>(Error.Failure("Wishlist.AlreadyExists", "This course is already in your wishlist"));
        }

        var wishlistItem = CourseWishlist.Create(courseId, userId);
        _context.Set<CourseWishlist>().Add(wishlistItem);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Course {CourseId} added to wishlist for user {UserId}", courseId, userId);
        return Result.Success(wishlistItem);
    }

    public async Task<Result<bool>> RemoveFromWishlistAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default)
    {
        var wishlistItem = await _context.Set<CourseWishlist>()
            .FirstOrDefaultAsync(w => w.CourseId == courseId && w.UserId == userId, cancellationToken).ConfigureAwait(false);

        if (wishlistItem == null)
        {
            return Result.Failure<bool>(Error.NotFound("Wishlist.NotFound", "This course is not in your wishlist"));
        }

        _context.Set<CourseWishlist>().Remove(wishlistItem);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Course {CourseId} removed from wishlist for user {UserId}", courseId, userId);
        return Result.Success(true);
    }

    public async Task<Result<IEnumerable<CourseWishlist>>> GetUserWishlistAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var items = await _context.Set<CourseWishlist>()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<IEnumerable<CourseWishlist>>(items);
    }

    public async Task<Result<bool>> IsInWishlistAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.Set<CourseWishlist>()
            .AnyAsync(w => w.CourseId == courseId && w.UserId == userId, cancellationToken).ConfigureAwait(false);

        return Result.Success(exists);
    }

    public async Task<Result<CourseWishlist>> UpdateWishlistPreferencesAsync(
        Guid courseId,
        Guid userId,
        bool notifyOnSale,
        bool notifyOnUpdate,
        CancellationToken cancellationToken = default)
    {
        var wishlistItem = await _context.Set<CourseWishlist>()
            .FirstOrDefaultAsync(w => w.CourseId == courseId && w.UserId == userId, cancellationToken).ConfigureAwait(false);

        if (wishlistItem == null)
        {
            return Result.Failure<CourseWishlist>(Error.NotFound("Wishlist.NotFound", "This course is not in your wishlist"));
        }

        // Note: Need to add setter methods to the entity for these properties
        // For now, return the existing item
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(wishlistItem);
    }
}
