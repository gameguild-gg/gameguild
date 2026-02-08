using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for RevokeTenantPermissionByIdCommand
/// </summary>
public sealed class RevokeTenantPermissionByIdHandler(IApplicationDbContext context)
    : ICommandHandler<RevokeTenantPermissionByIdCommand>
{
    public async Task<Unit> Handle(RevokeTenantPermissionByIdCommand request, CancellationToken cancellationToken)
    {
        var grant = await context.Set<TenantPermission>()
            .FirstOrDefaultAsync(g => g.Id == request.GrantId, cancellationToken)
            ?? throw new InvalidOperationException($"Tenant permission grant {request.GrantId} not found");

        context.Set<TenantPermission>().Remove(grant);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
