using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Experience.Social.Services;

/// <summary>
/// Service implementation for discussion reply operations
/// </summary>
public class ReplyService : IReplyService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ReplyService> _logger;

    public ReplyService(
        IApplicationDbContext context,
        ILogger<ReplyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<DiscussionReply>> CreateReplyAsync(
        Guid discussionId,
        Guid authorId,
        string content,
        Guid? parentReplyId = null,
        CancellationToken cancellationToken = default)
    {
        var discussion = await _context.Set<CourseDiscussion>()
            .FirstOrDefaultAsync(d => d.Id == discussionId, cancellationToken).ConfigureAwait(false);

        if (discussion == null)
        {
            return Result.Failure<DiscussionReply>(Error.NotFound("Discussion.NotFound", $"Discussion with ID {discussionId} not found"));
        }

        var reply = DiscussionReply.Create(discussionId, authorId, content, parentReplyId);
        _context.Set<DiscussionReply>().Add(reply);

        discussion.IncrementReplies();
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Reply created for discussion {DiscussionId} by user {AuthorId}", discussionId, authorId);
        return Result.Success(reply);
    }

    public async Task<Result<IEnumerable<DiscussionReply>>> GetDiscussionRepliesAsync(
        Guid discussionId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var replies = await _context.Set<DiscussionReply>()
            .Where(r => r.DiscussionId == discussionId)
            .OrderBy(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success<IEnumerable<DiscussionReply>>(replies);
    }

    public async Task<Result<DiscussionReply>> AcceptReplyAsAnswerAsync(
        Guid replyId,
        Guid discussionAuthorId,
        CancellationToken cancellationToken = default)
    {
        var reply = await _context.Set<DiscussionReply>()
            .FirstOrDefaultAsync(r => r.Id == replyId, cancellationToken).ConfigureAwait(false);

        if (reply == null)
        {
            return Result.Failure<DiscussionReply>(Error.NotFound("Reply.NotFound", $"Reply with ID {replyId} not found"));
        }

        // Verify the requester is the discussion author
        var discussion = await _context.Set<CourseDiscussion>()
            .FirstOrDefaultAsync(d => d.Id == reply.DiscussionId, cancellationToken).ConfigureAwait(false);

        if (discussion == null || discussion.AuthorId != discussionAuthorId)
        {
            return Result.Failure<DiscussionReply>(Error.Failure("Reply.Unauthorized", "Only the discussion author can accept answers"));
        }

        reply.AcceptAsAnswer();
        discussion.MarkResolved();
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Reply {ReplyId} accepted as answer for discussion {DiscussionId}", replyId, reply.DiscussionId);
        return Result.Success(reply);
    }

    public async Task<Result<DiscussionReply>> UpvoteReplyAsync(Guid replyId, CancellationToken cancellationToken = default)
    {
        var reply = await _context.Set<DiscussionReply>()
            .FirstOrDefaultAsync(r => r.Id == replyId, cancellationToken).ConfigureAwait(false);

        if (reply == null)
        {
            return Result.Failure<DiscussionReply>(Error.NotFound("Reply.NotFound", $"Reply with ID {replyId} not found"));
        }

        reply.Upvote();
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(reply);
    }

    public async Task<Result<bool>> DeleteReplyAsync(Guid replyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var reply = await _context.Set<DiscussionReply>()
            .FirstOrDefaultAsync(r => r.Id == replyId, cancellationToken).ConfigureAwait(false);

        if (reply == null)
        {
            return Result.Failure<bool>(Error.NotFound("Reply.NotFound", $"Reply with ID {replyId} not found"));
        }

        if (reply.AuthorId != userId)
        {
            return Result.Failure<bool>(Error.Failure("Reply.Unauthorized", "You can only delete your own replies"));
        }

        _context.Set<DiscussionReply>().Remove(reply);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Reply {ReplyId} deleted by user {UserId}", replyId, userId);
        return Result.Success(true);
    }
}
