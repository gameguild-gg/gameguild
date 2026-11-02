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
        Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken);

    /// <summary>Executes a mutation with an additional result-acceptance predicate.</summary>
    Task<Result<T>> ExecuteAsync<T>(Guid actorId, Guid? tenantId,
        Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken,
        Func<T, bool>? shouldCommit);

    /// <summary>Executes a mutation with explicit acceptance and entity-tenant audit-scope policies.</summary>
    Task<Result<T>> ExecuteAsync<T>(Guid actorId, Guid? tenantId,
        Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken,
        Func<T, bool>? shouldCommit, bool useEntityTenantScope);

    /// <summary>
    ///     Executes named system maintenance through the same transaction algorithm.
    ///     The host verifies the real system actor; the descriptor cannot authorize itself.
    /// </summary>
    Task<Result<T>> ExecuteSystemAsync<T>(PermissionSystemMutation maintenance, Guid? tenantId,
        Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken,
        Func<T, bool>? shouldCommit, bool useEntityTenantScope);
}
