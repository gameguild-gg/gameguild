namespace GameGuild.Learning.Experience.Social.Services;

/// <summary>
/// Service interface for course discussion operations
/// </summary>
public interface IDiscussionService
{
    /// <summary>
    /// Creates a new discussion thread
    /// </summary>
    Task<Result<CourseDiscussion>> CreateDiscussionAsync(
        Guid courseId,
        Guid authorId,
        string title,
        string content,
        Guid? contentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a discussion by ID
    /// </summary>
    Task<Result<CourseDiscussion>> GetDiscussionByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets discussions for a course
    /// </summary>
    Task<Result<IEnumerable<CourseDiscussion>>> GetCourseDiscussionsAsync(
        Guid courseId,
        int skip = 0,
        int take = 20,
        bool pinnedFirst = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets discussions for specific content within a course
    /// </summary>
    Task<Result<IEnumerable<CourseDiscussion>>> GetContentDiscussionsAsync(
        Guid courseId,
        Guid contentId,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pins a discussion (instructor/admin action)
    /// </summary>
    Task<Result<CourseDiscussion>> PinDiscussionAsync(Guid discussionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unpins a discussion
    /// </summary>
    Task<Result<CourseDiscussion>> UnpinDiscussionAsync(Guid discussionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a discussion as resolved
    /// </summary>
    Task<Result<CourseDiscussion>> MarkDiscussionResolvedAsync(Guid discussionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a discussion
    /// </summary>
    Task<Result<bool>> DeleteDiscussionAsync(Guid discussionId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments discussion view count
    /// </summary>
    Task<Result<CourseDiscussion>> IncrementDiscussionViewsAsync(Guid discussionId, CancellationToken cancellationToken = default);
}
