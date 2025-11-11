namespace GameGuild.Permissions.Domain.Enums;

/// <summary>
///     Represents the status of a resource invitation.
/// </summary>
public enum InvitationStatus
{
    /// <summary>
    ///     Invitation is pending a response.
    /// </summary>
    Pending = 0,

    /// <summary>
    ///     Invitation has been accepted by the invitee.
    /// </summary>
    Accepted = 1,

    /// <summary>
    ///     Invitation has been declined by the invitee.
    /// </summary>
    Declined = 2,

    /// <summary>
    ///     Invitation has been revoked by the inviter or administrator.
    /// </summary>
    Revoked = 3,

    /// <summary>
    ///     Invitation has expired before being accepted or declined.
    /// </summary>
    Expired = 4
}
