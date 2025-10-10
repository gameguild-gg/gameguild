using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Content.Versioning;

/// <summary>
/// Content versioning service interface.
/// </summary>
public interface IContentVersioningService
{
    Task<ContentVersion> CreateDraftAsync(
        Guid contentId,
        string contentType,
        string title,
        string body,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    Task<ContentVersion> UpdateDraftAsync(
        Guid versionId,
        string? title = null,
        string? body = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    Task<ContentVersion> SubmitForReviewAsync(
        Guid versionId,
        Guid authorId,
        string? reviewNotes = null,
        CancellationToken cancellationToken = default);

    Task<ContentReview> AssignReviewerAsync(
        Guid versionId,
        Guid reviewerId,
        CancellationToken cancellationToken = default);

    Task<ContentReview> SubmitReviewAsync(
        Guid reviewId,
        ContentReviewDecision decision,
        string? feedback = null,
        Dictionary<string, string>? suggestions = null,
        CancellationToken cancellationToken = default);

    Task<ContentVersion> PublishAsync(
        Guid versionId,
        Guid publishedBy,
        DateTime? scheduledFor = null,
        CancellationToken cancellationToken = default);

    Task<ContentVersion> RollbackAsync(
        Guid contentId,
        Guid targetVersionId,
        Guid rolledBackBy,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ContentVersion>> GetVersionHistoryAsync(
        Guid contentId,
        CancellationToken cancellationToken = default);

    Task<ContentDiff> CompareVersionsAsync(
        Guid versionId1,
        Guid versionId2,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ContentVersion>> GetPendingReviewsAsync(
        Guid? reviewerId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Content versioning service implementation.
/// </summary>
public sealed class ContentVersioningService : IContentVersioningService
{
    private readonly ILogger<ContentVersioningService> _logger;
    private readonly Dictionary<Guid, ContentVersion> _versions;
    private readonly Dictionary<Guid, ContentReview> _reviews;
    private readonly Dictionary<Guid, List<Guid>> _contentVersions;

    public ContentVersioningService(ILogger<ContentVersioningService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _versions = new Dictionary<Guid, ContentVersion>();
        _reviews = new Dictionary<Guid, ContentReview>();
        _contentVersions = new Dictionary<Guid, List<Guid>>();
    }

    public Task<ContentVersion> CreateDraftAsync(
        Guid contentId,
        string contentType,
        string title,
        string body,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var version = new ContentVersion
        {
            Id = Guid.NewGuid(),
            ContentId = contentId,
            ContentType = contentType,
            VersionNumber = GetNextVersionNumber(contentId),
            Title = title,
            Body = body,
            Metadata = metadata ?? new Dictionary<string, object>(),
            Status = ContentVersionStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            Reviews = new List<ContentReview>()
        };

        _versions[version.Id] = version;

        if (!_contentVersions.ContainsKey(contentId))
        {
            _contentVersions[contentId] = new List<Guid>();
        }
        _contentVersions[contentId].Add(version.Id);

        _logger.LogInformation("Created draft version {VersionId} for content {ContentId}",
            version.Id, contentId);

        return Task.FromResult(version);
    }

    public Task<ContentVersion> UpdateDraftAsync(
        Guid versionId,
        string? title = null,
        string? body = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (!_versions.TryGetValue(versionId, out var version))
        {
            throw new InvalidOperationException($"Version {versionId} not found");
        }

        if (version.Status != ContentVersionStatus.Draft)
        {
            throw new InvalidOperationException("Can only update draft versions");
        }

        if (title != null) version.Title = title;
        if (body != null) version.Body = body;
        if (metadata != null) version.Metadata = metadata;

        version.LastModifiedAt = DateTime.UtcNow;

        _logger.LogInformation("Updated draft version {VersionId}", versionId);

        return Task.FromResult(version);
    }

    public Task<ContentVersion> SubmitForReviewAsync(
        Guid versionId,
        Guid authorId,
        string? reviewNotes = null,
        CancellationToken cancellationToken = default)
    {
        if (!_versions.TryGetValue(versionId, out var version))
        {
            throw new InvalidOperationException($"Version {versionId} not found");
        }

        if (version.Status != ContentVersionStatus.Draft)
        {
            throw new InvalidOperationException("Can only submit draft versions for review");
        }

        version.Status = ContentVersionStatus.PendingReview;
        version.SubmittedForReviewAt = DateTime.UtcNow;
        version.SubmittedBy = authorId;
        version.ReviewNotes = reviewNotes;

        _logger.LogInformation("Version {VersionId} submitted for review by {AuthorId}",
            versionId, authorId);

        return Task.FromResult(version);
    }

    public Task<ContentReview> AssignReviewerAsync(
        Guid versionId,
        Guid reviewerId,
        CancellationToken cancellationToken = default)
    {
        if (!_versions.TryGetValue(versionId, out var version))
        {
            throw new InvalidOperationException($"Version {versionId} not found");
        }

        var review = new ContentReview
        {
            Id = Guid.NewGuid(),
            VersionId = versionId,
            ReviewerId = reviewerId,
            Status = ContentReviewStatus.Assigned,
            AssignedAt = DateTime.UtcNow
        };

        _reviews[review.Id] = review;
        version.Reviews.Add(review);

        _logger.LogInformation("Assigned reviewer {ReviewerId} to version {VersionId}",
            reviewerId, versionId);

        return Task.FromResult(review);
    }

    public Task<ContentReview> SubmitReviewAsync(
        Guid reviewId,
        ContentReviewDecision decision,
        string? feedback = null,
        Dictionary<string, string>? suggestions = null,
        CancellationToken cancellationToken = default)
    {
        if (!_reviews.TryGetValue(reviewId, out var review))
        {
            throw new InvalidOperationException($"Review {reviewId} not found");
        }

        if (!_versions.TryGetValue(review.VersionId, out var version))
        {
            throw new InvalidOperationException($"Version {review.VersionId} not found");
        }

        review.Decision = decision;
        review.Feedback = feedback;
        review.Suggestions = suggestions ?? new Dictionary<string, string>();
        review.CompletedAt = DateTime.UtcNow;
        review.Status = ContentReviewStatus.Completed;

        version.Status = decision switch
        {
            ContentReviewDecision.Approved => ContentVersionStatus.Approved,
            ContentReviewDecision.Rejected => ContentVersionStatus.Rejected,
            ContentReviewDecision.NeedsRevision => ContentVersionStatus.NeedsRevision,
            _ => throw new ArgumentException($"Unknown decision: {decision}")
        };

        _logger.LogInformation("Review {ReviewId} completed with decision {Decision}",
            reviewId, decision);

        return Task.FromResult(review);
    }

    public Task<ContentVersion> PublishAsync(
        Guid versionId,
        Guid publishedBy,
        DateTime? scheduledFor = null,
        CancellationToken cancellationToken = default)
    {
        if (!_versions.TryGetValue(versionId, out var version))
        {
            throw new InvalidOperationException($"Version {versionId} not found");
        }

        if (version.Status != ContentVersionStatus.Approved)
        {
            throw new InvalidOperationException("Can only publish approved versions");
        }

        version.Status = scheduledFor.HasValue
            ? ContentVersionStatus.Scheduled
            : ContentVersionStatus.Published;
        version.PublishedAt = scheduledFor ?? DateTime.UtcNow;
        version.PublishedBy = publishedBy;

        _logger.LogInformation("Version {VersionId} published by {PublishedBy}",
            versionId, publishedBy);

        return Task.FromResult(version);
    }

    public Task<ContentVersion> RollbackAsync(
        Guid contentId,
        Guid targetVersionId,
        Guid rolledBackBy,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (!_versions.TryGetValue(targetVersionId, out var targetVersion))
        {
            throw new InvalidOperationException($"Target version {targetVersionId} not found");
        }

        if (targetVersion.ContentId != contentId)
        {
            throw new InvalidOperationException("Target version does not belong to the specified content");
        }

        var rollbackVersion = new ContentVersion
        {
            Id = Guid.NewGuid(),
            ContentId = contentId,
            ContentType = targetVersion.ContentType,
            VersionNumber = GetNextVersionNumber(contentId),
            Title = targetVersion.Title,
            Body = targetVersion.Body,
            Metadata = new Dictionary<string, object>(targetVersion.Metadata)
            {
                ["RolledBackFrom"] = targetVersionId,
                ["RollbackReason"] = reason ?? "Not specified"
            },
            Status = ContentVersionStatus.Published,
            CreatedAt = DateTime.UtcNow,
            PublishedAt = DateTime.UtcNow,
            PublishedBy = rolledBackBy,
            Reviews = new List<ContentReview>()
        };

        _versions[rollbackVersion.Id] = rollbackVersion;
        _contentVersions[contentId].Add(rollbackVersion.Id);

        _logger.LogInformation("Rolled back content {ContentId} to version {TargetVersionId}",
            contentId, targetVersionId);

        return Task.FromResult(rollbackVersion);
    }

    public Task<IEnumerable<ContentVersion>> GetVersionHistoryAsync(
        Guid contentId,
        CancellationToken cancellationToken = default)
    {
        if (!_contentVersions.TryGetValue(contentId, out var versionIds))
        {
            return Task.FromResult<IEnumerable<ContentVersion>>(Array.Empty<ContentVersion>());
        }

        var versions = versionIds
            .Select(id => _versions[id])
            .OrderByDescending(v => v.CreatedAt);

        return Task.FromResult<IEnumerable<ContentVersion>>(versions.ToList());
    }

    public Task<ContentDiff> CompareVersionsAsync(
        Guid versionId1,
        Guid versionId2,
        CancellationToken cancellationToken = default)
    {
        if (!_versions.TryGetValue(versionId1, out var version1))
        {
            throw new InvalidOperationException($"Version {versionId1} not found");
        }

        if (!_versions.TryGetValue(versionId2, out var version2))
        {
            throw new InvalidOperationException($"Version {versionId2} not found");
        }

        var diff = new ContentDiff
        {
            Version1Id = versionId1,
            Version2Id = versionId2,
            TitleChanged = version1.Title != version2.Title,
            BodyChanged = version1.Body != version2.Body,
            TitleDiff = version1.Title != version2.Title
                ? $"Old: {version1.Title}\nNew: {version2.Title}"
                : null,
            BodyDiff = version1.Body != version2.Body
                ? GenerateSimpleDiff(version1.Body, version2.Body)
                : null,
            MetadataChanges = GetMetadataChanges(version1.Metadata, version2.Metadata)
        };

        return Task.FromResult(diff);
    }

    public Task<IEnumerable<ContentVersion>> GetPendingReviewsAsync(
        Guid? reviewerId = null,
        CancellationToken cancellationToken = default)
    {
        var pendingVersions = _versions.Values
            .Where(v => v.Status == ContentVersionStatus.PendingReview)
            .Where(v => !reviewerId.HasValue ||
                       v.Reviews.Any(r => r.ReviewerId == reviewerId.Value &&
                                         r.Status == ContentReviewStatus.Assigned));

        return Task.FromResult<IEnumerable<ContentVersion>>(pendingVersions.ToList());
    }

    private int GetNextVersionNumber(Guid contentId)
    {
        if (!_contentVersions.TryGetValue(contentId, out var versionIds) || versionIds.Count == 0)
        {
            return 1;
        }

        return versionIds.Select(id => _versions[id].VersionNumber).Max() + 1;
    }

    private static string GenerateSimpleDiff(string oldText, string newText)
    {
        return $"--- Old Content ---\n{oldText}\n\n+++ New Content +++\n{newText}";
    }

    private static Dictionary<string, object> GetMetadataChanges(
        Dictionary<string, object> old,
        Dictionary<string, object> new_)
    {
        var changes = new Dictionary<string, object>();

        foreach (var key in old.Keys.Union(new_.Keys))
        {
            var oldValue = old.TryGetValue(key, out var ov) ? ov : null;
            var newValue = new_.TryGetValue(key, out var nv) ? nv : null;

            if (!Equals(oldValue, newValue))
            {
                changes[key] = new { Old = oldValue, New = newValue };
            }
        }

        return changes;
    }
}

/// <summary>
/// Content version entity.
/// </summary>
public sealed class ContentVersion
{
    public required Guid Id { get; init; }
    public required Guid ContentId { get; init; }
    public required string ContentType { get; init; }
    public required int VersionNumber { get; init; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public required Dictionary<string, object> Metadata { get; set; }
    public required ContentVersionStatus Status { get; set; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? LastModifiedAt { get; set; }
    public DateTime? SubmittedForReviewAt { get; set; }
    public Guid? SubmittedBy { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? PublishedBy { get; set; }
    public required List<ContentReview> Reviews { get; init; }
}

/// <summary>
/// Content review entity.
/// </summary>
public sealed class ContentReview
{
    public required Guid Id { get; init; }
    public required Guid VersionId { get; init; }
    public required Guid ReviewerId { get; init; }
    public required ContentReviewStatus Status { get; set; }
    public ContentReviewDecision? Decision { get; set; }
    public string? Feedback { get; set; }
    public Dictionary<string, string>? Suggestions { get; set; }
    public required DateTime AssignedAt { get; init; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Content diff result.
/// </summary>
public sealed class ContentDiff
{
    public required Guid Version1Id { get; init; }
    public required Guid Version2Id { get; init; }
    public required bool TitleChanged { get; init; }
    public required bool BodyChanged { get; init; }
    public string? TitleDiff { get; init; }
    public string? BodyDiff { get; init; }
    public required Dictionary<string, object> MetadataChanges { get; init; }
}

/// <summary>
/// Content version status.
/// </summary>
public enum ContentVersionStatus
{
    Draft,
    PendingReview,
    NeedsRevision,
    Approved,
    Rejected,
    Scheduled,
    Published,
    Archived
}

/// <summary>
/// Content review status.
/// </summary>
public enum ContentReviewStatus
{
    Assigned,
    InProgress,
    Completed
}

/// <summary>
/// Content review decision.
/// </summary>
public enum ContentReviewDecision
{
    Approved,
    Rejected,
    NeedsRevision
}
