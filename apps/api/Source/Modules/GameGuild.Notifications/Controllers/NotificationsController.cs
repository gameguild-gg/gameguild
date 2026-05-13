using GameGuild.Identity.Context.Actors;
using GameGuild.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Notifications.Controllers;

/// <summary>
/// API controller for managing user notifications
/// </summary>
[Microsoft.AspNetCore.Http.Tags("notifications")]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notificationService;
    private readonly IActorContextAccessor _actorContextAccessor;

    public NotificationsController(
        INotificationService notificationService,
        IActorContextAccessor actorContextAccessor)
    {
        _notificationService = notificationService;
        _actorContextAccessor = actorContextAccessor;
    }

    private Guid GetRequiredUserId()
    {
        var actor = _actorContextAccessor.ActorContext;
        if (!actor.SubjectIdAsGuid.HasValue)
            throw new UnauthorizedAccessException("User must be authenticated");
        return actor.SubjectIdAsGuid.Value;
    }

    /// <summary>
    /// Gets the current user's notifications
    /// </summary>
    /// <param name="skip">Number of notifications to skip for pagination</param>
    /// <param name="take">Number of notifications to return</param>
    /// <param name="isRead">Filter by read status (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _notificationService.GetUserNotificationsAsync(userId, skip, take, isRead, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        var dtos = result.Value.Select(MapToDto);
        return Ok(dtos);
    }

    /// <summary>
    /// Gets the unread notification count for the current user
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadCountResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _notificationService.GetUnreadCountAsync(userId, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(new UnreadCountResponse(result.Value));
    }

    /// <summary>
    /// Gets a specific notification by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNotification(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _notificationService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return NotFound(result.Error);
        }

        // Verify the notification belongs to the current user
        var userId = GetRequiredUserId();
        if (result.Value.RecipientId != userId)
        {
            return Forbid();
        }

        return Ok(MapToDto(result.Value));
    }

    /// <summary>
    /// Marks a notification as read
    /// </summary>
    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken = default)
    {
        // First verify ownership
        var notificationResult = await _notificationService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (!notificationResult.IsSuccess)
        {
            return NotFound(notificationResult.Error);
        }

        var userId = GetRequiredUserId();
        if (notificationResult.Value.RecipientId != userId)
        {
            return Forbid();
        }

        var result = await _notificationService.MarkAsReadAsync(id, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Marks all notifications as read for the current user
    /// </summary>
    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _notificationService.MarkAllAsReadAsync(userId, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Marks a notification as unread
    /// </summary>
    [HttpPost("{id:guid}/unread")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsUnread(Guid id, CancellationToken cancellationToken = default)
    {
        var notificationResult = await _notificationService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (!notificationResult.IsSuccess)
        {
            return NotFound(notificationResult.Error);
        }

        var userId = GetRequiredUserId();
        if (notificationResult.Value.RecipientId != userId)
        {
            return Forbid();
        }

        var result = await _notificationService.MarkAsUnreadAsync(id, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes a notification
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNotification(Guid id, CancellationToken cancellationToken = default)
    {
        var notificationResult = await _notificationService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (!notificationResult.IsSuccess)
        {
            return NotFound(notificationResult.Error);
        }

        var userId = GetRequiredUserId();
        if (notificationResult.Value.RecipientId != userId)
        {
            return Forbid();
        }

        var result = await _notificationService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes all read notifications for the current user
    /// </summary>
    [HttpDelete("read")]
    [ProducesResponseType(typeof(DeletedCountResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteReadNotifications(CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _notificationService.DeleteReadNotificationsAsync(userId, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(new DeletedCountResponse(result.Value));
    }

    /// <summary>
    /// Gets the current user's notification preferences
    /// </summary>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(NotificationPreferenceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreferences(CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _notificationService.GetPreferencesAsync(userId, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(MapPreferenceToDto(result.Value));
    }

    /// <summary>
    /// Updates the current user's notification preferences
    /// </summary>
    [HttpPut("preferences")]
    [ProducesResponseType(typeof(NotificationPreferenceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdatePreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _notificationService.UpdatePreferencesAsync(
            userId,
            request.EmailEnabled,
            request.PushEnabled,
            request.InAppEnabled,
            request.SmsEnabled,
            request.MarketingEnabled,
            request.SocialEnabled,
            request.LearningEnabled,
            request.AchievementsEnabled,
            cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(MapPreferenceToDto(result.Value));
    }

    /// <summary>
    /// Sets quiet hours for the current user
    /// </summary>
    [HttpPut("preferences/quiet-hours")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetQuietHours(
        [FromBody] SetQuietHoursRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredUserId();
        var result = await _notificationService.SetQuietHoursAsync(
            userId,
            request.Start,
            request.End,
            request.Timezone,
            cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    #region Private Helpers

    private static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto(
            notification.Id,
            notification.Type.ToString(),
            notification.Channel.ToString(),
            notification.Title,
            notification.Message,
            notification.ActionUrl,
            notification.IconUrl,
            notification.IsRead,
            notification.ReadAt,
            notification.Priority.ToString(),
            notification.ReferenceEntityId,
            notification.ReferenceEntityType,
            notification.CreatedAt);
    }

    private static NotificationPreferenceDto MapPreferenceToDto(NotificationPreference preference)
    {
        return new NotificationPreferenceDto(
            preference.EmailEnabled,
            preference.PushEnabled,
            preference.InAppEnabled,
            preference.SmsEnabled,
            preference.MarketingEnabled,
            preference.SocialEnabled,
            preference.LearningEnabled,
            preference.AchievementsEnabled,
            preference.QuietHoursStart,
            preference.QuietHoursEnd,
            preference.Timezone,
            preference.EmailDigestFrequency?.ToString());
    }

    #endregion
}

#region DTOs

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Channel,
    string Title,
    string Message,
    string? ActionUrl,
    string? IconUrl,
    bool IsRead,
    DateTime? ReadAt,
    string Priority,
    Guid? ReferenceEntityId,
    string? ReferenceEntityType,
    DateTime CreatedAt);

public sealed record NotificationPreferenceDto(
    bool EmailEnabled,
    bool PushEnabled,
    bool InAppEnabled,
    bool SmsEnabled,
    bool MarketingEnabled,
    bool SocialEnabled,
    bool LearningEnabled,
    bool AchievementsEnabled,
    TimeOnly? QuietHoursStart,
    TimeOnly? QuietHoursEnd,
    string? Timezone,
    string? EmailDigestFrequency);

public sealed record UnreadCountResponse(int Count);

public sealed record DeletedCountResponse(int DeletedCount);

public sealed record UpdatePreferencesRequest(
    bool? EmailEnabled,
    bool? PushEnabled,
    bool? InAppEnabled,
    bool? SmsEnabled,
    bool? MarketingEnabled,
    bool? SocialEnabled,
    bool? LearningEnabled,
    bool? AchievementsEnabled);

public sealed record SetQuietHoursRequest(
    TimeOnly? Start,
    TimeOnly? End,
    string? Timezone);

#endregion
