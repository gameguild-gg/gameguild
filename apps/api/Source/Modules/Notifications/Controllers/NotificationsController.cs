using Microsoft.AspNetCore.Mvc;
using GameGuild.Authorization.Identity;

namespace GameGuild.Modules.Notifications;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<NotificationResponseDto>> GetNotifications()
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        var query = new NotificationQueryDto { Skip = 0, Take = 20 };
        var result = await _notificationService.GetNotificationsAsync(userId.Value, query);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        var count = await _notificationService.GetUnreadCountAsync(userId.Value);
        return Ok(count);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        var result = await _notificationService.MarkAsReadAsync(id, userId.Value);
        return result ? NoContent() : NotFound();
    }
}
