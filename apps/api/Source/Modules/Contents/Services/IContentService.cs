using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameGuild.Modules.Contents.Models;

using ContentEntity = GameGuild.Modules.Contents.Models.Content;
namespace GameGuild.Modules.Contents.Services;

/// <summary>
/// Service interface for content management operations
/// </summary>
public interface IContentService
{
    /// <summary>
    /// Creates new content
    /// </summary>
    Task<ContentEntity> CreateContentAsync(ContentEntity content);

    /// <summary>
    /// Updates existing content
    /// </summary>
    Task<ContentEntity> UpdateContentAsync(ContentEntity content);

    /// <summary>
    /// Deletes content (soft delete)
    /// </summary>
    Task<bool> DeleteContentAsync(Guid contentId);

    /// <summary>
    /// Gets content by ID
    /// </summary>
    Task<ContentEntity?> GetContentByIdAsync(Guid contentId);

    /// <summary>
    /// Gets content by slug
    /// </summary>
    Task<ContentEntity?> GetContentBySlugAsync(string slug);

    /// <summary>
    /// Gets all content for a tenant
    /// </summary>
    Task<IEnumerable<ContentEntity>> GetContentByTenantAsync(Guid tenantId);

    /// <summary>
    /// Gets content by author
    /// </summary>
    Task<IEnumerable<ContentEntity>> GetContentByAuthorAsync(Guid authorId);

    /// <summary>
    /// Gets content by type
    /// </summary>
    Task<IEnumerable<ContentEntity>> GetContentByTypeAsync(ContentType type);

    /// <summary>
    /// Gets content by status
    /// </summary>
    Task<IEnumerable<ContentEntity>> GetContentByStatusAsync(ContentStatus status);

    /// <summary>
    /// Publishes content
    /// </summary>
    Task<ContentEntity> PublishContentAsync(Guid contentId, Guid publishedBy);

    /// <summary>
    /// Schedules content for publishing
    /// </summary>
    Task<ContentEntity> ScheduleContentAsync(Guid contentId, DateTime scheduledPublishAt);

    /// <summary>
    /// Archives content
    /// </summary>
    Task<ContentEntity> ArchiveContentAsync(Guid contentId);

    /// <summary>
    /// Approves content
    /// </summary>
    Task<ContentEntity> ApproveContentAsync(Guid contentId, Guid approvedBy, string? approvalNotes);

    /// <summary>
    /// Rejects content
    /// </summary>
    Task<ContentEntity> RejectContentAsync(Guid contentId, Guid reviewedBy, string? reviewNotes);

    /// <summary>
    /// Submits content for review
    /// </summary>
    Task<ContentEntity> SubmitForReviewAsync(Guid contentId);

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
    Task<IEnumerable<ContentEntity>> SearchContentAsync(string searchTerm, Guid? tenantId = null);

    /// <summary>
    /// Gets published content with filters
    /// </summary>
    Task<IEnumerable<ContentEntity>> GetPublishedContentAsync(Guid? tenantId = null, ContentType? type = null, int? limit = null);

    /// <summary>
    /// Gets scheduled content that needs to be published
    /// </summary>
    Task<IEnumerable<ContentEntity>> GetScheduledContentReadyToPublishAsync();

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
