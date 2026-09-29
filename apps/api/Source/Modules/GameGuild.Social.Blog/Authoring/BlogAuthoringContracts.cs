namespace GameGuild.Social.Blog.Authoring;

/// <summary>Request to start an AI authoring run against a post draft revision.</summary>
/// <param name="ConversationId">Optional existing conversation to continue.</param>
/// <param name="PostRevision">The post revision the instruction was written against.</param>
/// <param name="Instruction">The author's natural-language instruction.</param>
/// <param name="ProposalKind">The kind of proposal the run must produce.</param>
/// <param name="Selection">Optional selected body text for context.</param>
/// <param name="IdempotencyKey">Client-supplied deduplication key.</param>
public sealed record BlogAiRunRequest(
    Guid? ConversationId,
    int PostRevision,
    string Instruction,
    BlogAiProposalKind ProposalKind,
    string? Selection,
    string IdempotencyKey);

/// <summary>A streamed authoring event persisted per run and replayed in sequence order.</summary>
public sealed record BlogAiStreamEventDto(
    long Sequence,
    string Type,
    string? Delta,
    Guid RunId,
    string Status,
    BlogAiCreditUsageDto? Usage = null,
    BlogAiProposalDto? Proposal = null,
    string? ErrorCode = null);

/// <summary>An AI-proposed change awaiting author apply/discard.</summary>
public sealed record BlogAiProposalDto(
    Guid Id,
    Guid RunId,
    int BasePostRevision,
    BlogAiProposalKind Kind,
    BlogAiProposalStatus Status,
    string OriginalContent,
    string ProposedContent,
    DateTimeOffset ProposedAt);

/// <summary>Credit accounting snapshot for a run or wallet.</summary>
public sealed record BlogAiCreditUsageDto(
    long AvailableSoftCredits,
    long MaximumEstimatedCost,
    int InputTokens,
    int OutputTokens,
    long SettledCost,
    long ReleasedAmount,
    string Currency = "SoftCoin");

/// <summary>A conversation message (user instruction or assistant output).</summary>
public sealed record BlogAiMessageDto(
    Guid Id,
    string Role,
    string Content,
    Guid? RunId,
    DateTimeOffset CreatedAt);

/// <summary>A copilot conversation scoped to one post and author.</summary>
public sealed record BlogAiConversationDto(
    Guid Id,
    Guid BlogPostId,
    Guid AuthorId,
    DateTimeOffset LastMessageAt,
    IReadOnlyList<BlogAiMessageDto> Messages);

/// <summary>A single AI authoring run with usage and (when complete) its proposal.</summary>
public sealed record BlogAiRunDto(
    Guid Id,
    Guid ConversationId,
    Guid BlogPostId,
    int BasePostRevision,
    BlogAiProposalKind ProposalKind,
    BlogAiRunStatus Status,
    string Instruction,
    string? Provider,
    string? Model,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    BlogAiCreditUsageDto Usage,
    BlogAiProposalDto? Proposal,
    string? ErrorCode,
    string? ErrorMessage);

/// <summary>The author's wallet snapshot for copilot UI badges.</summary>
public sealed record BlogAiEntitlementDto(
    long AvailableSoftCredits,
    long ReservedSoftCredits,
    long SettledSoftCredits,
    string Currency = "SoftCoin");

/// <summary>Applies a pending proposal to the post at the given revision (with cursor insert offset).</summary>
public sealed record ApplyBlogAiProposalRequest(int PostRevision, int? CursorOffset);
