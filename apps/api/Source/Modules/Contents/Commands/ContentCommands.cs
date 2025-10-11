using System;
using GameGuild.CQRS;
using GameGuild.Modules.Contents.Models;

namespace GameGuild.Modules.Contents.Commands;

/// <summary>
/// CQRS Commands for Content Management
/// </summary>

// Create content command
public record CreateContentCommand(
    Guid TenantId,
    Guid AuthorId,
    string Title,
    string Slug,
    string? Summary,
    string? Body,
    ContentType Type,
    AccessLevel Visibility,
    string? FeaturedImageUrl,
    string? License,
    string? Tags,
    string? CategoryIds,
    string? Metadata
) : IRequest<Result<Content>>;

// Update content command
public record UpdateContentCommand(
    Guid ContentId,
    string Title,
    string? Summary,
    string? Body,
    AccessLevel? Visibility,
    string? FeaturedImageUrl,
    string? License,
    string? Tags,
    string? CategoryIds,
    string? Metadata
) : IRequest<Result<Content>>;

// Delete content command
public record DeleteContentCommand(
    Guid ContentId
) : IRequest<Result<bool>>;

// Publish content command
public record PublishContentCommand(
    Guid ContentId,
    Guid PublishedBy
) : IRequest<Result<Content>>;

// Schedule content command
public record ScheduleContentCommand(
    Guid ContentId,
    DateTime ScheduledPublishAt
) : IRequest<Result<Content>>;

// Archive content command
public record ArchiveContentCommand(
    Guid ContentId
) : IRequest<Result<Content>>;

// Approve content command
public record ApproveContentCommand(
    Guid ContentId,
    Guid ApprovedBy,
    string? ApprovalNotes
) : IRequest<Result<Content>>;

// Reject content command
public record RejectContentCommand(
    Guid ContentId,
    Guid ReviewedBy,
    string? ReviewNotes
) : IRequest<Result<Content>>;

// Submit for review command
public record SubmitContentForReviewCommand(
    Guid ContentId
) : IRequest<Result<Content>>;

// Create version command
public record CreateContentVersionCommand(
    Guid ContentId,
    Guid CreatedBy,
    string? ChangeNotes
) : IRequest<Result<ContentVersion>>;

// Update engagement metrics command
public record UpdateEngagementMetricsCommand(
    Guid ContentId,
    int? LikeCount,
    int? CommentCount,
    int? ShareCount
) : IRequest<Result<bool>>;

// Process scheduled publishing command
public record ProcessScheduledPublishingCommand() : IRequest<Result<int>>;
