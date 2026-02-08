namespace GameGuild.Social.Ratings;

/// <summary>
/// Handles core rating CRUD operations (create, read, update, delete).
/// </summary>
public interface IRatingCrudService
{
    /// <summary>Create or update a rating for an entity</summary>
    Task<Result<Rating>> RateAsync(
        Guid entityId,
        string entityType,
        int value,
        string? reviewText = null,
        string? reviewTitle = null,
        CancellationToken ct = default);

    /// <summary>Get a specific rating by ID</summary>
    Task<Result<Rating>> GetByIdAsync(Guid ratingId, CancellationToken ct = default);

    /// <summary>Get the current user's rating for an entity</summary>
    Task<Result<Rating>> GetUserRatingAsync(Guid entityId, string entityType, CancellationToken ct = default);

    /// <summary>Delete the current user's rating</summary>
    Task<Result> DeleteAsync(Guid ratingId, CancellationToken ct = default);
}
