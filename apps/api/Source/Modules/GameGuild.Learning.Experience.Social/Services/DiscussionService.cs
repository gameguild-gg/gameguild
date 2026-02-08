using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Experience.Social.Services;

/// <summary>
/// Service implementation for course discussion operations
/// </summary>
public class DiscussionService : IDiscussionService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<DiscussionService> _logger;

    public DiscussionService(
        IApplicationDbContext context,
        ILogger<DiscussionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<CourseDiscussion>> CreateDiscussionAsync(
        Guid courseId,
        Guid authorId,
        string title,
        string content,
        Guid? contentId = null,
        CancellationToken cancellationToken = default)
    {
        var discussion = CourseDiscussion.Create(courseId, authorId, title, content, contentId);
        _context.Set<CourseDiscussion>().Add(discussion);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Discussion created for course {CourseId} by user {AuthorId}: {Title}", courseId, authorId, title);
        return Result.Success(discussion);
    }

    public async Task<Result<CourseDiscussion>> GetDiscussionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var discussion = await _context.Set<CourseDiscussion>()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken).ConfigureAwait(false);

        if (discussion == null)
        {
            return Result.Failure<CourseDiscussion>(Error.NotFound("Discussion.NotFound", $"Discussion with ID {id} not found"));
        }

        return Result.Success(discussion);
    }

    public async Task<Result<IEnumerable<CourseDiscussion>>> GetCourseDiscussionsAsync(
        Guid courseId,
        int skip = 0,
        int take = 20,
        bool pinnedFirst = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<CourseDiscussion>()
            .Where(d => d.CourseId == courseId);

        if (pinnedFirst)
        {
            query = query.OrderByDescending(d => d.IsPinned)
                .ThenByDescending(d => d.LastActivityAt);
        }
        else
        {
            query = query.OrderByDescending(d => d.LastActivityAt);
        }

        var discussions = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<IEnumerable<CourseDiscussion>>(discussions);
    }

    public async Task<Result<IEnumerable<CourseDiscussion>>> GetContentDiscussionsAsync(
        Guid courseId,
        Guid contentId,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var discussions = await _context.Set<CourseDiscussion>()
            .Where(d => d.CourseId == courseId && d.ContentId == contentId)
            .OrderByDescending(d => d.IsPinned)
            .ThenByDescending(d => d.LastActivityAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<CourseDiscussion>>(discussions);
    }

    public async Task<Result<CourseDiscussion>> PinDiscussionAsync(Guid discussionId, CancellationToken cancellationToken = default)
    {
        var discussion = await _context.Set<CourseDiscussion>()
            .FirstOrDefaultAsync(d => d.Id == discussionId, cancellationToken).ConfigureAwait(false);

        if (discussion == null)
        {
            return Result.Failure<CourseDiscussion>(Error.NotFound("Discussion.NotFound", $"Discussion with ID {discussionId} not found"));
        }

        discussion.Pin();
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Discussion {DiscussionId} pinned", discussionId);
        return Result.Success(discussion);
    }

    public async Task<Result<CourseDiscussion>> UnpinDiscussionAsync(Guid discussionId, CancellationToken cancellationToken = default)
    {
        var discussion = await _context.Set<CourseDiscussion>()
            .FirstOrDefaultAsync(d => d.Id == discussionId, cancellationToken).ConfigureAwait(false);

        if (discussion == null)
        {
            return Result.Failure<CourseDiscussion>(Error.NotFound("Discussion.NotFound", $"Discussion with ID {discussionId} not found"));
        }

        discussion.Unpin();
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Discussion {DiscussionId} unpinned", discussionId);
        return Result.Success(discussion);
    }

    public async Task<Result<CourseDiscussion>> MarkDiscussionResolvedAsync(Guid discussionId, CancellationToken cancellationToken = default)
    {
        var discussion = await _context.Set<CourseDiscussion>()
            .FirstOrDefaultAsync(d => d.Id == discussionId, cancellationToken).ConfigureAwait(false);

        if (discussion == null)
        {
            return Result.Failure<CourseDiscussion>(Error.NotFound("Discussion.NotFound", $"Discussion with ID {discussionId} not found"));
        }

        discussion.MarkResolved();
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Discussion {DiscussionId} marked as resolved", discussionId);
        return Result.Success(discussion);
    }

    public async Task<Result<bool>> DeleteDiscussionAsync(Guid discussionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var discussion = await _context.Set<CourseDiscussion>()
            .FirstOrDefaultAsync(d => d.Id == discussionId, cancellationToken).ConfigureAwait(false);

        if (discussion == null)
        {
            return Result.Failure<bool>(Error.NotFound("Discussion.NotFound", $"Discussion with ID {discussionId} not found"));
        }

        if (discussion.AuthorId != userId)
        {
            return Result.Failure<bool>(Error.Failure("Discussion.Unauthorized", "You can only delete your own discussions"));
        }

        _context.Set<CourseDiscussion>().Remove(discussion);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Discussion {DiscussionId} deleted by user {UserId}", discussionId, userId);
        return Result.Success(true);
    }

    public async Task<Result<CourseDiscussion>> IncrementDiscussionViewsAsync(Guid discussionId, CancellationToken cancellationToken = default)
    {
        var discussion = await _context.Set<CourseDiscussion>()
            .FirstOrDefaultAsync(d => d.Id == discussionId, cancellationToken).ConfigureAwait(false);

        if (discussion == null)
        {
            return Result.Failure<CourseDiscussion>(Error.NotFound("Discussion.NotFound", $"Discussion with ID {discussionId} not found"));
        }

        discussion.IncrementViews();
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(discussion);
    }
}
