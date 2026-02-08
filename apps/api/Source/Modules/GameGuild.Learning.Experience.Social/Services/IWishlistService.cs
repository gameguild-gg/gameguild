namespace GameGuild.Learning.Experience.Social.Services;

/// <summary>
/// Service interface for course wishlist (bookmark) operations
/// </summary>
public interface IWishlistService
{
    /// <summary>
    /// Adds a course to user's wishlist
    /// </summary>
    Task<Result<CourseWishlist>> AddToWishlistAsync(
        Guid courseId,
        Guid userId,
        bool notifyOnSale = true,
        bool notifyOnUpdate = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a course from user's wishlist
    /// </summary>
    Task<Result<bool>> RemoveFromWishlistAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user's wishlist
    /// </summary>
    Task<Result<IEnumerable<CourseWishlist>>> GetUserWishlistAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a course is in user's wishlist
    /// </summary>
    Task<Result<bool>> IsInWishlistAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates wishlist notification preferences
    /// </summary>
    Task<Result<CourseWishlist>> UpdateWishlistPreferencesAsync(
        Guid courseId,
        Guid userId,
        bool notifyOnSale,
        bool notifyOnUpdate,
        CancellationToken cancellationToken = default);
}
