namespace GameGuild.Learning.Experience.Social.Services;

/// <summary>
/// Service interface for discussion reply operations
/// </summary>
public interface IReplyService
{
    /// <summary>
    /// Creates a reply to a discussion
    /// </summary>
    Task<Result<DiscussionReply>> CreateReplyAsync(
        Guid discussionId,
        Guid authorId,
        string content,
        Guid? parentReplyId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets replies for a discussion
    /// </summary>
    Task<Result<IEnumerable<DiscussionReply>>> GetDiscussionRepliesAsync(
        Guid discussionId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts a reply as the answer
    /// </summary>
    Task<Result<DiscussionReply>> AcceptReplyAsAnswerAsync(
        Guid replyId,
        Guid discussionAuthorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upvotes a reply
    /// </summary>
    Task<Result<DiscussionReply>> UpvoteReplyAsync(Guid replyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a reply
    /// </summary>
    Task<Result<bool>> DeleteReplyAsync(Guid replyId, Guid userId, CancellationToken cancellationToken = default);
}
