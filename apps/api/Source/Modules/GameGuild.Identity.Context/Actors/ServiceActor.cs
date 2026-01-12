namespace GameGuild.Identity.Context.Actors;

/// <summary>
///     Represents a service or application acting on its own behalf (service-to-service communication).
/// </summary>
public sealed record ServiceActor : IActor
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ServiceActor"/> record.
    /// </summary>
    /// <param name="serviceId">The unique service/client identifier.</param>
    /// <param name="serviceName">The human-readable name of the service.</param>
    /// <param name="scopes">The OAuth scopes or permissions granted to this service.</param>
    public ServiceActor(string serviceId, string serviceName, IReadOnlySet<string>? scopes = null)
    {
        ServiceId = serviceId ?? throw new ArgumentNullException(nameof(serviceId));
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        Scopes = scopes ?? (IReadOnlySet<string>)new HashSet<string>();
    }

    /// <summary>
    ///     Gets the unique service/client identifier.
    /// </summary>
    public string ServiceId { get; }

    /// <summary>
    ///     Gets the human-readable name of the service.
    /// </summary>
    public string ServiceName { get; }

    /// <summary>
    ///     Gets the OAuth scopes or permissions granted to this service.
    /// </summary>
    public IReadOnlySet<string>? Scopes { get; }

    /// <inheritdoc />
    public ActorKind Kind => ActorKind.Service;

    /// <inheritdoc />
    public string SubjectId => ServiceId;

    /// <inheritdoc />
    public string DisplayName => ServiceName;
}
