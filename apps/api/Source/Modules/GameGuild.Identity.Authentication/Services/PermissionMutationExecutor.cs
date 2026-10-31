using GameGuild.CQRS;
using GameGuild.Identity.Authorization;

namespace GameGuild.Identity.Authentication;

/// <summary>Retains the authentication callers entry point to the shared permission transaction.</summary>
internal sealed class PermissionMutationExecutor(IApplicationDbContext context)
{
    private readonly IPermissionMutationExecutor _executor = new PermissionMutationTransaction(context);

    public Task<Result<T>> ExecuteAsync<T>(Guid actorId, Guid? tenantId,
        Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken,
        Func<T, bool>? shouldCommit = null, bool useEntityTenantScope = false) =>
        _executor.ExecuteAsync(actorId, tenantId, operation, cancellationToken, shouldCommit, useEntityTenantScope);
}
