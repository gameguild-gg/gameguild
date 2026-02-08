using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Experience.Social.Services;

/// <summary>
/// Service implementation for course like (social proof) operations
/// </summary>
public class LikeService : ILikeService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<LikeService> _logger;

    public LikeService(
        IApplicationDbContext context,
        ILogger<LikeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<CourseLike>> LikeCourseAsync(Guid courseId, Guid userId, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Set<CourseLike>()
            .FirstOrDefaultAsync(l => l.CourseId == courseId && l.UserId == userId, cancellationToken).ConfigureAwait(false);

        if (existing != null)
        {
            return Result.Failure<CourseLike>(Error.Failure("Like.AlreadyExists", "You have already liked this course"));
        }

        var like = CourseLike.Create(courseId, userId, tenantId);
        _context.Set<CourseLike>().Add(like);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Course {CourseId} liked by user {UserId}", courseId, userId);
        return Result.Success(like);
    }

    public async Task<Result<bool>> UnlikeCourseAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default)
    {
        var like = await _context.Set<CourseLike>()
            .FirstOrDefaultAsync(l => l.CourseId == courseId && l.UserId == userId, cancellationToken).ConfigureAwait(false);

        if (like == null)
        {
            return Result.Failure<bool>(Error.NotFound("Like.NotFound", "You haven't liked this course"));
        }

        _context.Set<CourseLike>().Remove(like);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Course {CourseId} unliked by user {UserId}", courseId, userId);
        return Result.Success(true);
    }

    public async Task<Result<bool>> HasUserLikedCourseAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.Set<CourseLike>()
            .AnyAsync(l => l.CourseId == courseId && l.UserId == userId, cancellationToken).ConfigureAwait(false);

        return Result.Success(exists);
    }

    public async Task<Result<int>> GetCourseLikeCountAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var count = await _context.Set<CourseLike>()
            .CountAsync(l => l.CourseId == courseId, cancellationToken).ConfigureAwait(false);

        return Result.Success(count);
    }

    public async Task<Result<IEnumerable<CourseLike>>> GetUserLikedCoursesAsync(
        Guid userId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var likes = await _context.Set<CourseLike>()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<IEnumerable<CourseLike>>(likes);
    }
}
