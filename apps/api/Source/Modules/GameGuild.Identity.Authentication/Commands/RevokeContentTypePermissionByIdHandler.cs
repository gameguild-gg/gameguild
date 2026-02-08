using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Handler for RevokeContentTypePermissionByIdCommand
/// </summary>
public sealed class RevokeContentTypePermissionByIdHandler(IApplicationDbContext context)
    : ICommandHandler<RevokeContentTypePermissionByIdCommand>
{
    public async Task<Unit> Handle(RevokeContentTypePermissionByIdCommand request, CancellationToken cancellationToken)
    {
        var grant = await context.Set<ContentTypePermission>()
            .FirstOrDefaultAsync(g => g.Id == request.GrantId, cancellationToken)
            ?? throw new InvalidOperationException($"Content type permission grant {request.GrantId} not found");

        context.Set<ContentTypePermission>().Remove(grant);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
