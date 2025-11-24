using GameGuild.CQRS;
using GameGuild.Tenants.Abstractions;

namespace GameGuild.Tenants.Commands;

/// <summary>
///     Handler for deleting tenant command
/// </summary>
public class DeleteTenantCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<DeleteTenantCommand>
{
    public async Task<Unit> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await tenantRepository.DeleteAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
