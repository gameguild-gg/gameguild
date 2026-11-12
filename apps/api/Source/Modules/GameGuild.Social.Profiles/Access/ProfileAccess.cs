using GameGuild.Identity.Context.Actors;

namespace GameGuild.Social.Profiles;

/// <summary>
///     Read-side port for profile visibility (issue #1315 SP1): resolves whether a viewer
///     holds an explicit follow relationship with a profile's user — the only entitlement,
///     besides ownership, through which a Private/Connections profile's details are served.
///     Declared here because Social.Follows already references Social.Profiles; the Follows
///     module registers the real adapter, and <see cref="NullProfileFollowerPort"/> is the
///     fail-closed default when the Follows module is not part of the composition.
/// </summary>
public interface IProfileFollowerPort
{
    Task<bool> IsFollowerAsync(Guid followerUserId, Guid profileUserId, CancellationToken ct = default);
}

/// <summary>
///     Fail-closed default: an unresolvable follow graph never grants Private-profile access.
/// </summary>
public sealed class NullProfileFollowerPort : IProfileFollowerPort
{
    public Task<bool> IsFollowerAsync(Guid followerUserId, Guid profileUserId, CancellationToken ct = default)
        => Task.FromResult(false);
}

/// <summary>
///     The viewer of a profile read, resolved from the ambient actor context. A null
///     <see cref="UserId"/> represents an anonymous caller.
/// </summary>
public readonly record struct ProfileViewer(Guid? UserId, bool IsSystemAdmin)
{
    public bool IsOwner(Guid profileUserId) => UserId is { } id && id == profileUserId;
}

/// <summary>Resolves <see cref="ProfileViewer"/> from the ambient actor context.</summary>
public static class ProfileViewerResolution
{
    public static ProfileViewer ToProfileViewer(this ActorContext actorContext)
        => new(actorContext?.SubjectIdAsGuid, actorContext?.IsSystemAdmin == true);
}
