namespace GameGuild.Identity.Context.Actors;

/// <summary>
///     Defines the type of actor making a request.
///     Used to distinguish between different authentication and authorization flows.
/// </summary>
public enum ActorKind
{
    /// <summary>
    ///     An anonymous or unauthenticated actor.
    /// </summary>
    Anonymous = 0,

    /// <summary>
    ///     A human user authenticated via standard authentication flows (JWT, OAuth, etc.).
    /// </summary>
    User = 1,

    /// <summary>
    ///     A service or application acting on its own behalf (service-to-service).
    ///     Typically authenticated via API keys, client credentials, or mTLS.
    /// </summary>
    Service = 2,

    /// <summary>
    ///     The system itself acting on behalf of background jobs, schedulers, or internal processes.
    ///     Has elevated privileges for system-level operations.
    /// </summary>
    System = 3,

    /// <summary>
    ///     An external webhook or integration callback.
    ///     May have limited, pre-configured permissions.
    /// </summary>
    Webhook = 4,

    /// <summary>
    ///     An external third-party actor (partner API, federated identity, etc.).
    ///     Subject to additional validation and restrictions.
    /// </summary>
    External = 5
}
