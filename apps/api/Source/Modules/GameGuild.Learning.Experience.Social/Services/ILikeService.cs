namespace GameGuild.Learning.Experience.Social.Services;

/// <summary>
/// Service interface for course like (social proof) operations
/// </summary>
public interface ILikeService
{
    /// <summary>
    /// Likes a course
    /// </summary>
    Task<Result<CourseLike>> LikeCourseAsync(Guid courseId, Guid userId, Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlikes a course
    /// </summary>
    Task<Result<bool>> UnlikeCourseAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has liked a course
    /// </summary>
    Task<Result<bool>> HasUserLikedCourseAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the like count for a course
    /// </summary>
    Task<Result<int>> GetCourseLikeCountAsync(Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all courses liked by a user
    /// </summary>
    Task<Result<IEnumerable<CourseLike>>> GetUserLikedCoursesAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);
}
