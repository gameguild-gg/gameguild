namespace GameGuild.Modules.Notifications;

/// <summary>
/// Service for sending notifications based on security events.
/// </summary>
public class SecurityEventNotificationService : ISecurityEventNotificationService
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<SecurityEventNotificationService> _logger;

    public SecurityEventNotificationService(
        INotificationService notificationService,
        ILogger<SecurityEventNotificationService> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotifyFailedLoginAsync(
        Guid userId,
        Guid tenantId,
        string ipAddress,
        int attemptCount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var priority = attemptCount >= 5 ? NotificationPriority.High : NotificationPriority.Normal;

            await _notificationService.SendNotificationAsync(
                new SendNotificationRequest
                {
                    UserId = userId,
                    TenantId = tenantId,
                    Type = NotificationType.Warning,
                    Priority = priority,
                    Title = "Failed Login Attempt",
                    Content = $"Failed login attempt detected from IP {ipAddress}. This is attempt #{attemptCount}. If this wasn't you, please secure your account immediately.",
                    Channels = NotificationChannel.InApp | NotificationChannel.Email,
                    Data = new Dictionary<string, object>
                    {
                        ["IpAddress"] = ipAddress,
                        ["AttemptCount"] = attemptCount,
                        ["EventType"] = "FailedLogin"
                    }
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send failed login notification for user {UserId}", userId);
        }
    }

    /// <inheritdoc />
    public async Task NotifyAccountLockoutAsync(
        Guid userId,
        Guid tenantId,
        string reason,
        DateTime lockoutEnd,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _notificationService.SendNotificationAsync(
                new SendNotificationRequest
                {
                    UserId = userId,
                    TenantId = tenantId,
                    Type = NotificationType.Error,
                    Priority = NotificationPriority.High,
                    Title = "Account Locked",
                    Content = $"Your account has been locked due to: {reason}. The lockout will end at {lockoutEnd:yyyy-MM-dd HH:mm:ss UTC}.",
                    Channels = NotificationChannel.InApp | NotificationChannel.Email,
                    Data = new Dictionary<string, object>
                    {
                        ["Reason"] = reason,
                        ["LockoutEnd"] = lockoutEnd,
                        ["EventType"] = "AccountLockout"
                    }
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send account lockout notification for user {UserId}", userId);
        }
    }

    /// <inheritdoc />
    public async Task NotifyPermissionChangedAsync(
        Guid userId,
        Guid tenantId,
        string permissionName,
        bool granted,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var action = granted ? "granted" : "revoked";
            var type = granted ? NotificationType.Success : NotificationType.Warning;

            await _notificationService.SendNotificationAsync(
                new SendNotificationRequest
                {
                    UserId = userId,
                    TenantId = tenantId,
                    Type = type,
                    Priority = NotificationPriority.Normal,
                    Title = $"Permission {action}",
                    Content = $"The permission '{permissionName}' has been {action} for your account.",
                    Channels = NotificationChannel.InApp | NotificationChannel.Email,
                    Data = new Dictionary<string, object>
                    {
                        ["PermissionName"] = permissionName,
                        ["Granted"] = granted,
                        ["EventType"] = "PermissionChanged"
                    }
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send permission changed notification for user {UserId}", userId);
        }
    }

    /// <inheritdoc />
    public async Task NotifyRoleAssignedAsync(
        Guid userId,
        Guid tenantId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _notificationService.SendNotificationAsync(
                new SendNotificationRequest
                {
                    UserId = userId,
                    TenantId = tenantId,
                    Type = NotificationType.Success,
                    Priority = NotificationPriority.Normal,
                    Title = "Role Assigned",
                    Content = $"You have been assigned the role '{roleName}'.",
                    Channels = NotificationChannel.InApp | NotificationChannel.Email,
                    Data = new Dictionary<string, object>
                    {
                        ["RoleName"] = roleName,
                        ["EventType"] = "RoleAssigned"
                    }
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send role assigned notification for user {UserId}", userId);
        }
    }

    /// <inheritdoc />
    public async Task NotifyRoleRevokedAsync(
        Guid userId,
        Guid tenantId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _notificationService.SendNotificationAsync(
                new SendNotificationRequest
                {
                    UserId = userId,
                    TenantId = tenantId,
                    Type = NotificationType.Warning,
                    Priority = NotificationPriority.Normal,
                    Title = "Role Revoked",
                    Content = $"The role '{roleName}' has been revoked from your account.",
                    Channels = NotificationChannel.InApp | NotificationChannel.Email,
                    Data = new Dictionary<string, object>
                    {
                        ["RoleName"] = roleName,
                        ["EventType"] = "RoleRevoked"
                    }
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send role revoked notification for user {UserId}", userId);
        }
    }

    /// <inheritdoc />
    public async Task NotifySuspiciousActivityAsync(
        Guid userId,
        Guid tenantId,
        string activityDescription,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _notificationService.SendNotificationAsync(
                new SendNotificationRequest
                {
                    UserId = userId,
                    TenantId = tenantId,
                    Type = NotificationType.Alert,
                    Priority = NotificationPriority.High,
                    Title = "Suspicious Activity Detected",
                    Content = $"Suspicious activity detected: {activityDescription} from IP {ipAddress}. If this wasn't you, please secure your account immediately.",
                    Channels = NotificationChannel.InApp | NotificationChannel.Email | NotificationChannel.Sms,
                    Data = new Dictionary<string, object>
                    {
                        ["ActivityDescription"] = activityDescription,
                        ["IpAddress"] = ipAddress,
                        ["EventType"] = "SuspiciousActivity"
                    }
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send suspicious activity notification for user {UserId}", userId);
        }
    }

    /// <inheritdoc />
    public async Task NotifyMfaEnabledAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _notificationService.SendNotificationAsync(
                new SendNotificationRequest
                {
                    UserId = userId,
                    TenantId = tenantId,
                    Type = NotificationType.Success,
                    Priority = NotificationPriority.Normal,
                    Title = "Multi-Factor Authentication Enabled",
                    Content = "Multi-factor authentication has been enabled for your account. Your account is now more secure.",
                    Channels = NotificationChannel.InApp | NotificationChannel.Email,
                    Data = new Dictionary<string, object>
                    {
                        ["EventType"] = "MfaEnabled"
                    }
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send MFA enabled notification for user {UserId}", userId);
        }
    }

    /// <inheritdoc />
    public async Task NotifyMfaDisabledAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _notificationService.SendNotificationAsync(
                new SendNotificationRequest
                {
                    UserId = userId,
                    TenantId = tenantId,
                    Type = NotificationType.Warning,
                    Priority = NotificationPriority.High,
                    Title = "Multi-Factor Authentication Disabled",
                    Content = "Multi-factor authentication has been disabled for your account. Your account security has been reduced. If you didn't do this, please contact support immediately.",
                    Channels = NotificationChannel.InApp | NotificationChannel.Email,
                    Data = new Dictionary<string, object>
                    {
                        ["EventType"] = "MfaDisabled"
                    }
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send MFA disabled notification for user {UserId}", userId);
        }
    }

    /// <inheritdoc />
    public async Task NotifyPasswordChangedAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _notificationService.SendNotificationAsync(
                new SendNotificationRequest
                {
                    UserId = userId,
                    TenantId = tenantId,
                    Type = NotificationType.Info,
                    Priority = NotificationPriority.Normal,
                    Title = "Password Changed",
                    Content = "Your password has been successfully changed. If you didn't make this change, please contact support immediately.",
                    Channels = NotificationChannel.InApp | NotificationChannel.Email,
                    Data = new Dictionary<string, object>
                    {
                        ["EventType"] = "PasswordChanged"
                    }
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password changed notification for user {UserId}", userId);
        }
    }

    /// <inheritdoc />
    public async Task NotifyAdminActionAsync(
        Guid userId,
        Guid tenantId,
        string actionDescription,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _notificationService.SendNotificationAsync(
                new SendNotificationRequest
                {
                    UserId = userId,
                    TenantId = tenantId,
                    Type = NotificationType.Info,
                    Priority = NotificationPriority.Normal,
                    Title = "Admin Action on Your Account",
                    Content = $"An administrator has performed the following action on your account: {actionDescription}",
                    Channels = NotificationChannel.InApp | NotificationChannel.Email,
                    Data = new Dictionary<string, object>
                    {
                        ["ActionDescription"] = actionDescription,
                        ["AdminUserId"] = adminUserId,
                        ["EventType"] = "AdminAction"
                    }
                },
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send admin action notification for user {UserId}", userId);
        }
    }
}
