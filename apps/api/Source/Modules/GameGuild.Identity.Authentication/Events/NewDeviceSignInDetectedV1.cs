namespace GameGuild.Identity.Authentication;

/// <summary>
///     A session was created for a user from a first-seen device fingerprint
///     (issue #1085). Unlike <see cref="NewLoginDetectedV1"/> (every sign-in,
///     strictly opt-in), a first-seen device is a security-relevant change and the
///     owner is notified through the host-side notification bridge. The payload is
///     redacted to non-personal data: the coarse parsed device summary and the coarse
///     session location — never the raw user agent, IP address, or precise location.
/// </summary>
public sealed record NewDeviceSignInDetectedV1(
    [property: NonPersonalEventData] Guid UserId,
    [property: NonPersonalEventData] Guid SessionId,
    [property: NonPersonalEventData] string DeviceSummary,
    [property: NonPersonalEventData] string LocationSummary) : DurableIntegrationEventBase
{
    public override string EventName => "identity.authentication.new-device-sign-in-detected.v1";

    public override string SourceModule => "Identity.Authentication";
}
