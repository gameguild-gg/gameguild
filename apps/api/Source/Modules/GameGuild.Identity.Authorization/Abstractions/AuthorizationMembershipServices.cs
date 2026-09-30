namespace GameGuild.Identity.Authorization;

/// <summary>Resolves the active group memberships that belong in an authorization token.</summary>
public interface IAuthorizationGroupMembershipProvider
{
    Task<IReadOnlyCollection<Guid>> GetActiveGroupIdsAsync(Guid userId, Guid? tenantId);

    Task<IReadOnlyCollection<Guid>> GetActiveGroupIdsAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken);
}

/// <summary>Invalidates a user's issued authorization tokens after membership changes.</summary>
public interface IUserAuthorizationTokenVersionService
{
    Task IncrementAsync(Guid userId);

    Task IncrementAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Advances authorization token versions for several users with one persistence operation.</summary>
    Task IncrementManyAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}
