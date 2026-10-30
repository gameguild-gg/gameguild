using GameGuild.CQRS;

namespace GameGuild.Identity.Authorization;

/// <summary>Executes guarded permission writes with relational rollback and audit-buffer isolation.</summary>
/// <remarks>Callers establish authorization and supply the trusted actor and mutation scope.</remarks>
public interface IPermissionMutationExecutor
{
    /// <summary>
    ///     Commits an accepted mutation or restores its database, tracked state and pending audit evidence.
    ///     Caller-owned transactions use savepoints and remain owned by the caller.
    /// </summary>
    Task<Result<T>> ExecuteAsync<T>(Guid actorId, Guid? tenantId,
        Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken,
        Func<T, bool>? shouldCommit = null, bool useEntityTenantScope = false);
}
