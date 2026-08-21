using GameGuild.CQRS;
using GameGuild.CQRS.Models;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Raised when a tenant invite email should be queued (new invite or resend). Consumed by the
///     Notifications module, which creates the email-channel Notification row (RecipientId=null, email-only).
/// </summary>
public class TenantInviteRequestedNotification(
    Guid tenantId,
    string inviteeEmail,
    string? inviteeName,
    string? invitedByEmail,
    string tenantName,
    string role,
    string reviewUrl,
    string activationUrl,
    bool resend) : DomainEvent
{
    public Guid TenantId { get; } = tenantId;

    public string InviteeEmail { get; } = inviteeEmail;

    public string? InviteeName { get; } = inviteeName;

    public string? InvitedByEmail { get; } = invitedByEmail;

    public string TenantName { get; } = tenantName;

    public string Role { get; } = role;

    public string ReviewUrl { get; } = reviewUrl;

    public string ActivationUrl { get; } = activationUrl;

    public bool Resend { get; } = resend;
}
