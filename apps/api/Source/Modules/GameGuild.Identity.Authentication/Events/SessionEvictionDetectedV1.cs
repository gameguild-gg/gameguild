namespace GameGuild.Identity.Authentication;

/// <summary>
///     An older active session was terminated because the account reached its concurrent
///     session limit when a new session was created (issue #1086). The payload is
///     redacted to non-personal identifiers; recipient data is loaded by the consumer,
///     and the owner notification is queued by the host-side notification bridge.
/// </summary>
public sealed record SessionEvictionDetectedV1(
    [property: NonPersonalEventData] Guid UserId,
    [property: NonPersonalEventData] Guid SessionId,
    [property: NonPersonalEventData] string DeviceSummary,
    [property: NonPersonalEventData] int MaxConcurrentSessions) : DurableIntegrationEventBase
{
    public override string EventName => "identity.authentication.session-eviction-detected.v1";

    public override string SourceModule => "Identity.Authentication";
}
