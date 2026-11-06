namespace GameGuild.Identity.Authentication;

/// <summary>
///     A password change or reset terminated the account's other sessions (issue #1091).
///     The host-side notification bridge turns this into the owner confirmation email
///     ("all other sessions were terminated because your password changed"). The payload
///     stays non-personal: no device, IP, or location data travels with the event.
/// </summary>
public sealed record OtherSessionsTerminatedV1(
    [property: NonPersonalEventData] Guid UserId,
    [property: NonPersonalEventData] string Reason,
    [property: NonPersonalEventData] int TerminatedSessionCount) : DurableIntegrationEventBase
{
    public override string EventName => "identity.authentication.other-sessions-terminated.v1";
    public override string SourceModule => "Identity.Authentication";
}
