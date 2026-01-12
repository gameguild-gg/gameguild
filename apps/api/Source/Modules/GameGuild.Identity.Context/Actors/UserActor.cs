namespace GameGuild.Identity.Context.Actors;

/// <summary>
///     Represents a human user actor authenticated via standard authentication flows.
/// </summary>
/// <param name="UserId">The unique user identifier (typically a GUID).</param>
/// <param name="Email">The user's email address, if available.</param>
/// <param name="Name">The user's display name, if available.</param>
public sealed record UserActor(
    Guid UserId,
    string? Email = null,
    string? Name = null
) : IActor
{
    /// <inheritdoc />
    public ActorKind Kind => ActorKind.User;

    /// <inheritdoc />
    public string SubjectId => UserId.ToString();

    /// <inheritdoc />
    public string DisplayName => Name ?? Email ?? UserId.ToString();

    /// <summary>
    ///     Creates a UserActor from a string subject ID.
    /// </summary>
    /// <param name="subjectId">The subject ID string (must be a valid GUID).</param>
    /// <param name="email">Optional email address.</param>
    /// <param name="name">Optional display name.</param>
    /// <returns>A new UserActor instance.</returns>
    /// <exception cref="ArgumentException">Thrown when subjectId is not a valid GUID.</exception>
    public static UserActor FromSubject(string subjectId, string? email = null, string? name = null)
    {
        if (!Guid.TryParse(subjectId, out var userId))
        {
            throw new ArgumentException($"Subject ID '{subjectId}' is not a valid GUID.", nameof(subjectId));
        }

        return new UserActor(userId, email, name);
    }
}
