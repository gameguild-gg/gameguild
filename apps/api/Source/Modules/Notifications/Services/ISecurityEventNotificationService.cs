using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameGuild.Modules.Notifications;

/// <summary>
/// Service for sending notifications based on security events.
/// </summary>
public interface ISecurityEventNotificationService
{
    /// <summary>
    /// Sends notification for failed login attempts.
    /// </summary>
    Task NotifyFailedLoginAsync(Guid userId, Guid tenantId, string ipAddress, int attemptCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends notification for account lockout.
    /// </summary>
    Task NotifyAccountLockoutAsync(Guid userId, Guid tenantId, string reason, DateTime lockoutEnd, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends notification for permission changes.
    /// </summary>
    Task NotifyPermissionChangedAsync(Guid userId, Guid tenantId, string permissionName, bool granted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends notification for role assignment.
    /// </summary>
    Task NotifyRoleAssignedAsync(Guid userId, Guid tenantId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends notification for role revocation.
    /// </summary>
    Task NotifyRoleRevokedAsync(Guid userId, Guid tenantId, string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends notification for suspicious activity detection.
    /// </summary>
    Task NotifySuspiciousActivityAsync(Guid userId, Guid tenantId, string activityDescription, string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends notification for MFA enabled.
    /// </summary>
    Task NotifyMfaEnabledAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends notification for MFA disabled.
    /// </summary>
    Task NotifyMfaDisabledAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends notification for password changed.
    /// </summary>
    Task NotifyPasswordChangedAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends notification for admin action on user account.
    /// </summary>
    Task NotifyAdminActionAsync(Guid userId, Guid tenantId, string actionDescription, Guid adminUserId, CancellationToken cancellationToken = default);
}
