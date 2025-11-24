using Asp.Versioning;
using GameGuild.CQRS;
using GameGuild.Users.Commands;
using GameGuild.Users.Models;
using GameGuild.Users.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Users;

/// <summary>
///     Controller for managing user notifications
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "user-notifications")]
[Tags("user-notifications")]
public sealed class UserNotificationsController(ISender sender) : ControllerBase
{
    /// <summary>
    ///     Get user notifications with pagination, search, and sorting
    /// </summary>
    [HttpGet("v{version:apiVersion}/users/{userId:guid}/notifications")]
    [EndpointSummary("Get user notifications with pagination, search, and sorting")]
    [ProducesResponseType<PagedResult<UserNotificationDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNotifications(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "desc",
        [FromQuery] bool? isRead = null,
        [FromQuery] bool? isArchived = null,
        [FromQuery] string? type = null,
        [FromQuery] string? priority = null,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken ct = default
    )
    {
        var query = new GetUserNotificationsPagedQuery(
            userId, search, sortBy, sortDirection, isRead, isArchived,
            type, priority, fromDate, toDate, page, pageSize);
        var result = await sender.Send(query, ct);

        return Ok(result);
    }

    /// <summary>
    ///     Mark multiple notifications as read for a user
    /// </summary>
    [HttpPost("v{version:apiVersion}/users/{userId:guid}/notifications:mark-as-read")]
    [EndpointSummary("Mark multiple notifications as read for a user")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarkNotificationsAsRead(Guid userId, [FromBody] BulkNotificationRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        if (body.NotificationIds == null || body.NotificationIds.Count == 0)
        {
            return BadRequest("NotificationIds cannot be empty");
        }

        var command = new BulkMarkNotificationsAsReadCommand(userId, body.NotificationIds);
        await sender.Send(command, ct);

        return NoContent();
    }

    /// <summary>
    ///     Mark multiple notifications as unread for a user
    /// </summary>
    [HttpPost("v{version:apiVersion}/users/{userId:guid}/notifications:mark-as-unread")]
    [EndpointSummary("Mark multiple notifications as unread for a user")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MarkNotificationsAsUnread(Guid userId, [FromBody] BulkNotificationRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        if (body.NotificationIds == null || body.NotificationIds.Count == 0)
        {
            return BadRequest("NotificationIds cannot be empty");
        }

        var command = new BulkMarkNotificationsAsUnreadCommand(userId, body.NotificationIds);
        await sender.Send(command, ct);

        return NoContent();
    }

    /// <summary>
    ///     Archive multiple notifications for a user
    /// </summary>
    [HttpPost("v{version:apiVersion}/users/{userId:guid}/notifications:archive")]
    [EndpointSummary("Archive multiple notifications for a user")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ArchiveNotifications(Guid userId, [FromBody] BulkNotificationRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        if (body.NotificationIds == null || body.NotificationIds.Count == 0)
        {
            return BadRequest("NotificationIds cannot be empty");
        }

        var command = new BulkArchiveNotificationsCommand(userId, body.NotificationIds);
        await sender.Send(command, ct);

        return NoContent();
    }

    /// <summary>
    ///     Unarchive multiple notifications for a user
    /// </summary>
    [HttpPost("v{version:apiVersion}/users/{userId:guid}/notifications:unarchive")]
    [EndpointSummary("Unarchive multiple notifications for a user")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnarchiveNotifications(Guid userId, [FromBody] BulkNotificationRequest body, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        if (body.NotificationIds == null || body.NotificationIds.Count == 0)
        {
            return BadRequest("NotificationIds cannot be empty");
        }

        var command = new BulkUnarchiveNotificationsCommand(userId, body.NotificationIds);
        await sender.Send(command, ct);

        return NoContent();
    }

    /// <summary>
    ///     Check if user notification exists
    /// </summary>
    [HttpHead("v{version:apiVersion}/users/{userId:guid}/notifications/{notificationId:guid}")]
    [EndpointSummary("Check if user notification exists")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckNotificationExists(Guid userId, Guid notificationId, CancellationToken ct)
    {
        var query = new GetUserNotificationQuery(userId, notificationId);
        var result = await sender.Send(query, ct);

        return result == null ? NotFound() : Ok();
    }

    /// <summary>
    ///     Get detailed notification by ID
    /// </summary>
    [HttpGet("v{version:apiVersion}/users/{userId:guid}/notifications/{notificationId:guid}")]
    [EndpointSummary("Get detailed notification by ID")]
    [ProducesResponseType<UserNotificationDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNotification(Guid userId, Guid notificationId, CancellationToken ct)
    {
        var query = new GetUserNotificationQuery(userId, notificationId);
        var result = await sender.Send(query, ct);

        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    ///     Mark notification as read
    /// </summary>
    [HttpPost("v{version:apiVersion}/users/{userId:guid}/notifications/{notificationId:guid}:mark-as-read")]
    [EndpointSummary("Mark notification as read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkNotificationAsRead(Guid userId, Guid notificationId, CancellationToken ct)
    {
        var command = new MarkNotificationAsReadCommand(userId, notificationId);
        await sender.Send(command, ct);

        return NoContent();
    }

    /// <summary>
    ///     Mark notification as unread
    /// </summary>
    [HttpPost("v{version:apiVersion}/users/{userId:guid}/notifications/{notificationId:guid}:mark-as-unread")]
    [EndpointSummary("Mark notification as unread")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkNotificationAsUnread(Guid userId, Guid notificationId, CancellationToken ct)
    {
        var command = new MarkNotificationAsUnreadCommand(userId, notificationId);
        await sender.Send(command, ct);

        return NoContent();
    }

    /// <summary>
    ///     Archive notification
    /// </summary>
    [HttpPost("v{version:apiVersion}/users/{userId:guid}/notifications/{notificationId:guid}:archive")]
    [EndpointSummary("Archive notification")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveNotification(Guid userId, Guid notificationId, CancellationToken ct)
    {
        var command = new ArchiveNotificationCommand(userId, notificationId);
        await sender.Send(command, ct);

        return NoContent();
    }

    /// <summary>
    ///     Unarchive notification
    /// </summary>
    [HttpPost("v{version:apiVersion}/users/{userId:guid}/notifications/{notificationId:guid}:unarchive")]
    [EndpointSummary("Unarchive notification")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnarchiveNotification(Guid userId, Guid notificationId, CancellationToken ct)
    {
        var command = new UnarchiveNotificationCommand(userId, notificationId);
        await sender.Send(command, ct);

        return NoContent();
    }
}
