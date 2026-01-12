namespace GameGuild.Identity.Context.Actors;

/// <summary>
///     Marker interface for all actor types.
///     An actor represents an authenticated identity making a request.
/// </summary>
/// <remarks>
///     Implement this interface for specific actor types (User, Service, System, etc.)
///     to enable polymorphic actor handling in authorization and auditing.
/// </remarks>
public interface IActor
{
    /// <summary>
    ///     Gets the kind of actor (User, Service, System, etc.).
    /// </summary>
    ActorKind Kind { get; }

    /// <summary>
    ///     Gets the unique identifier for this actor.
    ///     For users, this is typically the user ID.
    ///     For services, this is the service/client ID.
    ///     For system actors, this may be a well-known constant.
    /// </summary>
    string SubjectId { get; }

    /// <summary>
    ///     Gets a display name for the actor for logging and auditing purposes.
    /// </summary>
    string DisplayName { get; }
}
