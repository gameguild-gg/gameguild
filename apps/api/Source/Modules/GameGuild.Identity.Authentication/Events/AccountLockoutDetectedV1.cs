namespace GameGuild.Identity.Authentication;

/// <summary>
///     The account lockout policy engaged for an account: repeated failed password
///     sign-in attempts reached the configured threshold and local sign-in is rejected
///     until <see cref="LockoutEndsAtUtc"/> (issue #1083). The payload is redacted to
///     non-personal identifiers; recipient data is loaded by the consumer, and the
///     owner notification is queued by the host-side notification bridge.
/// </summary>
public sealed record AccountLockoutDetectedV1(
    [property: NonPersonalEventData] Guid UserId,
    [property: NonPersonalEventData] DateTime LockoutEndsAtUtc,
    [property: NonPersonalEventData] int FailedAttemptThreshold) : DurableIntegrationEventBase
{
    public override string EventName => "identity.authentication.account-lockout-detected.v1";

    public override string SourceModule => "Identity.Authentication";
}
