using GameGuild.Modules.Notifications.Dtos;
using GameGuild.Modules.Notifications.Services;
using Microsoft.AspNetCore.Mvc;


namespace GameGuild.Modules.Notifications.Controllers;

/// <summary> API controller for notification management </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase {
  private readonly INotificationService _notificationService;

  public NotificationsController(INotificationService notificationService) { _notificationService = notificationService; }

  /// <summary> Get user's notifications with filtering and pagination </summary>
  [HttpGet]
  public async Task<ActionResult<NotificationResponseDto>> GetNotifications([FromQuery] NotificationQueryDto query) {
    var userId = User.GetUserId();
    var result = await _notificationService.GetNotificationsAsync(userId, query);

    return Ok(result);
  }

  /// <summary> Get a specific notification by ID </summary>
  [HttpGet("{id:guid}")]
  public async Task<ActionResult<NotificationDto>> GetNotification(Guid id) {
    var userId = User.GetUserId();
    var notification = await _notificationService.GetNotificationByIdAsync(id, userId);

    if (notification == null) return NotFound();

    return Ok(notification);
  }

  /// <summary> Create a new notification (admin/system use) </summary>
  [HttpPost]
  // [RequireRole("Admin", "System")]
  public async Task<ActionResult<NotificationDto>> CreateNotification([FromBody] CreateNotificationDto dto) {
    var notification = await _notificationService.CreateNotificationAsync(dto);

    return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification);
  }

  /// <summary> Create multiple notifications in bulk (admin/system use) </summary>
  [HttpPost("bulk")]
  // [RequireRole("Admin", "System")]
  public async Task<ActionResult<List<NotificationDto>>> CreateBulkNotifications([FromBody] List<CreateNotificationDto> dtos) {
    var notifications = await _notificationService.CreateBulkNotificationsAsync(dtos);

    return Ok(notifications);
  }

  /// <summary> Mark a notification as read </summary>
  [HttpPatch("{id:guid}/read")]
  public async Task<ActionResult> MarkAsRead(Guid id) {
    var userId = User.GetUserId();
    var success = await _notificationService.MarkAsReadAsync(id, userId);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary> Mark all notifications as read </summary>
  [HttpPatch("read-all")]
  public async Task<ActionResult<int>> MarkAllAsRead() {
    var userId = User.GetUserId();
    var count = await _notificationService.MarkAllAsReadAsync(userId);

    return Ok(count);
  }

  /// <summary> Archive a notification </summary>
  [HttpPatch("{id:guid}/archive")]
  public async Task<ActionResult> ArchiveNotification(Guid id) {
    var userId = User.GetUserId();
    var success = await _notificationService.ArchiveNotificationAsync(id, userId);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary> Toggle star status of a notification </summary>
  [HttpPatch("{id:guid}/star")]
  public async Task<ActionResult> ToggleStar(Guid id) {
    var userId = User.GetUserId();
    var success = await _notificationService.ToggleStarAsync(id, userId);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary> Delete a notification </summary>
  [HttpDelete("{id:guid}")]
  public async Task<ActionResult> DeleteNotification(Guid id) {
    var userId = User.GetUserId();
    var success = await _notificationService.DeleteNotificationAsync(id, userId);

    if (!success) return NotFound();

    return NoContent();
  }

  /// <summary> Get unread notification count </summary>
  [HttpGet("unread-count")]
  public async Task<ActionResult<int>> GetUnreadCount() {
    var userId = User.GetUserId();
    var count = await _notificationService.GetUnreadCountAsync(userId);

    return Ok(count);
  }

  /// <summary> Perform bulk action on multiple notifications </summary>
  [HttpPatch("bulk-action")]
  public async Task<ActionResult<int>> BulkAction([FromBody] BulkNotificationActionDto dto) {
    var userId = User.GetUserId();
    var count = await _notificationService.BulkActionAsync(userId, dto);

    return Ok(count);
  }

  /// <summary> Get user's notification preferences </summary>
  [HttpGet("preferences")]
  public async Task<ActionResult<NotificationPreferencesDto>> GetPreferences() {
    var userId = User.GetUserId();
    var preferences = await _notificationService.GetPreferencesAsync(userId);

    return Ok(preferences);
  }

  /// <summary> Update user's notification preferences </summary>
  [HttpPut("preferences")]
  public async Task<ActionResult<NotificationPreferencesDto>> UpdatePreferences([FromBody] NotificationPreferencesDto dto) {
    var userId = User.GetUserId();
    var updatedPreferences = await _notificationService.UpdatePreferencesAsync(userId, dto);

    return Ok(updatedPreferences);
  }

  /// <summary> Clean up old archived notifications (admin use) </summary>
  [HttpDelete("cleanup")]
  // [RequireRole("Admin")]
  public async Task<ActionResult<int>> CleanupOldNotifications([FromQuery] DateTime? olderThan = null) {
    var cutoffDate = olderThan ?? DateTime.UtcNow.AddMonths(-6); // Default to 6 months
    var count = await _notificationService.CleanupOldNotificationsAsync(cutoffDate);

    return Ok(count);
  }
}
