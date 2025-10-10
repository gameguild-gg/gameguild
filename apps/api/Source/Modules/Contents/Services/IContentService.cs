using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameGuild.Source.Modules.Contents.Services;

/// <summary>
/// Service interface for content management operations
/// </summary>
public interface IContentService
{
    /// <summary>
    /// Creates new content
    /// </summary>
    Task<Content> CreateContentAsync(Content content);

    /// <summary>
    /// Updates existing content
    /// </summary>
    Task<Content> UpdateContentAsync(Content content);

    /// <summary>
    /// Deletes content (soft delete)
    /// </summary>
    Task<bool> DeleteContentAsync(Guid contentId);

    /// <summary>
    /// Gets content by ID
    /// </summary>
    Task<Content?> GetContentByIdAsync(Guid contentId);

    /// <summary>
    /// Gets content by slug
    /// </summary>
    Task<Content?> GetContentBySlugAsync(string slug);

    /// <summary>
    /// Gets all content for a tenant
    /// </summary>
    Task<IEnumerable<Content>> GetContentByTenantAsync(Guid tenantId);

    /// <summary>
    /// Gets content by author
    /// </summary>
    Task<IEnumerable<Content>> GetContentByAuthorAsync(Guid authorId);

    /// <summary>
    /// Gets content by type
    /// </summary>
    Task<IEnumerable<Content>> GetContentByTypeAsync(ContentType type);

    /// <summary>
    /// Gets content by status
    /// </summary>
    Task<IEnumerable<Content>> GetContentByStatusAsync(ContentStatus status);

    /// <summary>
    /// Publishes content
    /// </summary>
    Task<Content> PublishContentAsync(Guid contentId, Guid publishedBy);

    /// <summary>
    /// Schedules content for publishing
    /// </summary>
    Task<Content> ScheduleContentAsync(Guid contentId, DateTime scheduledPublishAt);

    /// <summary>
    /// Archives content
    /// </summary>
    Task<Content> ArchiveContentAsync(Guid contentId);

    /// <summary>
    /// Approves content
    /// </summary>
    Task<Content> ApproveContentAsync(Guid contentId, Guid approvedBy, string? approvalNotes);

    /// <summary>
    /// Rejects content
    /// </summary>
    Task<Content> RejectContentAsync(Guid contentId, Guid reviewedBy, string? reviewNotes);

    /// <summary>
    /// Submits content for review
    /// </summary>
    Task<Content> SubmitForReviewAsync(Guid contentId);

    /// <summary>
    /// Creates a new version of content
    /// </summary>
    Task<ContentVersion> CreateVersionAsync(Guid contentId, Guid createdBy, string? changeNotes);

    /// <summary>
    /// Gets all versions of content
    /// </summary>
    Task<IEnumerable<ContentVersion>> GetVersionsAsync(Guid contentId);

    /// <summary>
    /// Gets a specific version of content
    /// </summary>
    Task<ContentVersion?> GetVersionAsync(Guid contentId, int version);

    /// <summary>
    /// Searches content by metadata, title, or body
    /// </summary>
    Task<IEnumerable<Content>> SearchContentAsync(string searchTerm, Guid? tenantId = null);

    /// <summary>
    /// Gets published content with filters
    /// </summary>
    Task<IEnumerable<Content>> GetPublishedContentAsync(Guid? tenantId = null, ContentType? type = null, int? limit = null);

    /// <summary>
    /// Gets scheduled content that needs to be published
    /// </summary>
    Task<IEnumerable<Content>> GetScheduledContentReadyToPublishAsync();

    /// <summary>
    /// Processes scheduled content publishing
    /// </summary>
    Task ProcessScheduledPublishingAsync();

    /// <summary>
    /// Increments view count for content
    /// </summary>
    Task IncrementViewCountAsync(Guid contentId);

    /// <summary>
    /// Updates engagement metrics (likes, comments, shares)
    /// </summary>
    Task UpdateEngagementMetricsAsync(Guid contentId, int? likeCount = null, int? commentCount = null, int? shareCount = null);
}
