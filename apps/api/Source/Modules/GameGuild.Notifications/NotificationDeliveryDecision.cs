namespace GameGuild.Notifications;

/// <summary>
/// Action chosen by preference evaluation for an outbound notification
/// </summary>
public enum NotificationDeliveryAction
{
    /// <summary>Deliver immediately (row Pending)</summary>
    Send = 0,

    /// <summary>Defer delivery until <see cref="NotificationDeliveryDecision.HeldUntil"/> (row Pending + ScheduledAt)</summary>
    HoldUntil = 1,

    /// <summary>Bundle into the user's email digest (row HeldForDigest)</summary>
    Digest = 2,

    /// <summary>Do not deliver (no row)</summary>
    Drop = 3
}

/// <summary>
/// Discriminated-union-style outcome of <see cref="Services.INotificationPreferenceService.DecideDeliveryAsync"/>:
/// Send, HoldUntil (quiet hours), Digest, or Drop (with reason).
/// </summary>
public sealed record NotificationDeliveryDecision
{
    private NotificationDeliveryDecision(NotificationDeliveryAction action, DateTime? heldUntil, string? reason)
    {
        Action = action;
        HeldUntil = heldUntil;
        Reason = reason;
    }

    /// <summary>Which action was chosen</summary>
    public NotificationDeliveryAction Action { get; }

    /// <summary>UTC instant delivery is held until (only for <see cref="NotificationDeliveryAction.HoldUntil"/>)</summary>
    public DateTime? HeldUntil { get; }

    /// <summary>Why the notification was dropped (only for <see cref="NotificationDeliveryAction.Drop"/>)</summary>
    public string? Reason { get; }

    public static NotificationDeliveryDecision Send() => new(NotificationDeliveryAction.Send, null, null);

    public static NotificationDeliveryDecision HoldUntil(DateTime heldUntil) => new(NotificationDeliveryAction.HoldUntil, heldUntil, null);

    public static NotificationDeliveryDecision Digest() => new(NotificationDeliveryAction.Digest, null, null);

    public static NotificationDeliveryDecision Drop(string reason) => new(NotificationDeliveryAction.Drop, null, reason);
}
