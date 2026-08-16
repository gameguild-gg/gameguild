using GameGuild.Notifications.Entities;
using GameGuild.Notifications.Services;
using GameGuild.SharedKernel;
using GameGuild.Social.Follows.Services;
using Microsoft.Extensions.Logging;

namespace GameGuild.Social.Posts.Services;

/// <summary>
/// Creates a community post and notifies followers when a course or project is published.
/// </summary>
public interface IPublicationAnnouncer
{
    Task AnnounceCoursePublishedAsync(Guid authorId, string courseTitle, Guid courseId, Guid? tenantId = null, CancellationToken cancellationToken = default);

    Task AnnounceProjectPublishedAsync(Guid authorId, string projectTitle, string projectSlug, Guid projectId, Guid? tenantId = null, CancellationToken cancellationToken = default);
}

public sealed class PublicationAnnouncerService(
    IPostCrudService postService,
    IFollowerService followerService,
    INotificationService notificationService,
    ILogger<PublicationAnnouncerService> logger) : IPublicationAnnouncer
{
    private const int MaxFanOut = 50;

    public async Task AnnounceCoursePublishedAsync(Guid authorId, string courseTitle, Guid courseId, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var content = $"📚 Just published a new course: {courseTitle}! Check it out and enroll: /courses/{courseId}";
        await AnnounceAsync(authorId, content, tenantId, courseId, "Course", "Course published", $"'{courseTitle}' is now live.", cancellationToken).ConfigureAwait(false);
    }

    public async Task AnnounceProjectPublishedAsync(Guid authorId, string projectTitle, string projectSlug, Guid projectId, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var slug = string.IsNullOrWhiteSpace(projectSlug) ? projectId.ToString() : projectSlug;
        var content = $"🚀 Just published the project: {projectTitle}! Take a look and share feedback: /projects/{slug}";
        await AnnounceAsync(authorId, content, tenantId, projectId, "Project", "Project published", $"'{projectTitle}' is now live.", cancellationToken).ConfigureAwait(false);
    }

    private async Task AnnounceAsync(Guid authorId, string content, Guid? tenantId, Guid entityId, string entityType, string title, string message, CancellationToken cancellationToken)
    {
        if (authorId == Guid.Empty)
        {
            return;
        }

        try
        {
            await postService.CreatePostAsync(authorId, content, PostVisibility.Public, tenantId: tenantId, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create publication post for {EntityType} {EntityId}", entityType, entityId);
        }

        await NotifyFollowersAsync(authorId, tenantId, entityId, entityType, title, message, cancellationToken).ConfigureAwait(false);
    }

    private async Task NotifyFollowersAsync(Guid authorId, Guid? tenantId, Guid entityId, string entityType, string title, string message, CancellationToken cancellationToken)
    {
        try
        {
            var followersResult = await followerService.GetFollowersAsync(authorId, "User", 0, MaxFanOut, cancellationToken).ConfigureAwait(false);
            if (!followersResult.IsSuccess || followersResult.Value is null)
            {
                return;
            }

            foreach (var follower in followersResult.Value)
            {
                if (follower.FollowerId == authorId)
                {
                    continue;
                }

                await notificationService.SendAsync(
                    follower.FollowerId,
                    NotificationType.System,
                    title,
                    message,
                    channel: NotificationChannel.InApp,
                    tenantId: tenantId,
                    actionUrl: entityId == Guid.Empty ? null : $"/{(entityType == "Course" ? "courses" : "projects")}/{entityId}",
                    referenceEntityId: entityId,
                    referenceEntityType: entityType,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to notify followers for {EntityType} {EntityId} publication", entityType, entityId);
        }
    }
}
