using GameGuild.Abstractions;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for RevokeResourcePermissionByIdCommand
/// </summary>
public sealed class RevokeResourcePermissionByIdHandler(IApplicationDbContext context)
    : ICommandHandler<RevokeResourcePermissionByIdCommand>
{
    public async Task<Unit> Handle(RevokeResourcePermissionByIdCommand request, CancellationToken cancellationToken)
    {
        var grant = await context.Set<GenericResourcePermission>()
            .FirstOrDefaultAsync(g => g.Id == request.GrantId, cancellationToken)
            ?? throw new InvalidOperationException($"Resource permission grant {request.GrantId} not found");

        context.Set<GenericResourcePermission>().Remove(grant);
        await context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
