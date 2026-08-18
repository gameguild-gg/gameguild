using GameGuild.Announcements.Contracts;
using GameGuild.CQRS;
using GameGuild;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using GameGuild.Social.Follows.Services;
using GameGuild.Social.Posts;
using GameGuild.Social.Posts.Services;
using Microsoft.Extensions.Logging;

namespace GameGuild.Social.Announcements.Services;

/// <summary>
/// Handles AnnouncePublicationCommand: creates the community post and notifies
/// the actor's followers (plus a targeted recipient where the kind has one).
/// </summary>
public sealed class PublicationAnnouncerHandler(
    IPostCrudService postService,
    IFollowerService followerService,
    INotificationService notificationService,
    ILogger<PublicationAnnouncerHandler> logger) : ICommandHandler<AnnouncePublicationCommand, Result>
{
    private const int MaxFanOut = 50;

    public async Task<Result> Handle(AnnouncePublicationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.ActorId == Guid.Empty)
            {
                return Result.Failure(Error.Validation("Announcements.NoActor", "Actor is required."));
            }

            await postService.CreatePostAsync(
                request.ActorId,
                BuildContent(request),
                PostVisibility.Public,
                tenantId: request.TenantId,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Announcements must never fail the domain operation that dispatched them.
            logger.LogWarning(ex, "Announcement post for {Kind} {EntityId} failed", request.Kind, request.EntityId);
        }

        try
        {
            await NotifyTargetRecipientAsync(request, cancellationToken).ConfigureAwait(false);
            await NotifyFollowersAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Announcement notifications for {Kind} {EntityId} failed", request.Kind, request.EntityId);
        }

        return Result.Success();
    }

    private static string BuildContent(AnnouncePublicationCommand request) => request.Kind switch
    {
        PublicationKind.CoursePublished =>
            $"📚 Just published a new course: {request.Title}! Check it out and enroll: /courses/{request.EntityId}",
        PublicationKind.ProjectPublished =>
            $"🚀 Just published the project: {request.Title}! Take a look and share feedback: /projects/{request.Slug ?? request.EntityId.ToString()}",
        PublicationKind.TestingEventCreated =>
            $"🧪 New testing event: {request.Title}! Event starts {request.StartsAt:MMM d, HH:mm 'UTC'}. Details: /testing-lab/events/{request.EntityId}",
        PublicationKind.ProjectJoinedTestingEvent =>
            $"🎮 '{request.Title}' just joined the testing event {request.SecondaryTitle}! Follow the build and share your feedback: /testing-lab/events/{request.EntityId}",
        _ => request.Title,
    };

    private (string Title, string Message, string ActionUrl, NotificationPriority Priority) Describe(AnnouncePublicationCommand request) => request.Kind switch
    {
        PublicationKind.CoursePublished => ("Course published", $"'{request.Title}' is now live.", $"/courses/{request.EntityId}", NotificationPriority.Normal),
        PublicationKind.ProjectPublished => ("Project published", $"'{request.Title}' is now live.", $"/projects/{request.Slug ?? request.EntityId.ToString()}", NotificationPriority.Normal),
        PublicationKind.TestingEventCreated => ("New testing event", $"'{request.Title}' opens {request.StartsAt:MMM d, HH:mm 'UTC'}.", $"/testing-lab/events/{request.EntityId}", NotificationPriority.Normal),
        PublicationKind.ProjectJoinedTestingEvent => ("Project joined testing", $"'{request.Title}' applied to '{request.SecondaryTitle}'.", $"/testing-lab/events/{request.EntityId}", NotificationPriority.Normal),
        _ => ("Update", request.Title, "/", NotificationPriority.Normal),
    };

    private async Task NotifyTargetRecipientAsync(AnnouncePublicationCommand request, CancellationToken cancellationToken)
    {
        if (request.NotifyUserId is not { } recipientId || recipientId == Guid.Empty || recipientId == request.ActorId)
        {
            return;
        }

        var description = Describe(request);
        await TrySendAsync(recipientId, request, description, cancellationToken).ConfigureAwait(false);
    }

    private async Task NotifyFollowersAsync(AnnouncePublicationCommand request, CancellationToken cancellationToken)
    {
        var followersResult = await followerService.GetFollowersAsync(request.ActorId, "User", 0, MaxFanOut, cancellationToken).ConfigureAwait(false);
        if (!followersResult.IsSuccess || followersResult.Value is null)
        {
            return;
        }

        var description = Describe(request);
        foreach (var follower in followersResult.Value)
        {
            if (follower.FollowerId == request.ActorId || follower.FollowerId == request.NotifyUserId)
            {
                continue;
            }
            await TrySendAsync(follower.FollowerId, request, description, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task TrySendAsync(Guid recipientId, AnnouncePublicationCommand request, (string Title, string Message, string ActionUrl, NotificationPriority Priority) description, CancellationToken cancellationToken)
    {
        try
        {
            await notificationService.SendAsync(
                recipientId,
                NotificationType.System,
                description.Title,
                description.Message,
                channel: NotificationChannel.InApp,
                tenantId: request.TenantId,
                actionUrl: description.ActionUrl,
                priority: description.Priority,
                referenceEntityId: request.EntityId,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Announcement notification to {RecipientId} failed", recipientId);
        }
    }
}
