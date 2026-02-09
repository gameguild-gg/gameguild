using GameGuild.CQRS;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Handler for deleting tenant command
/// </summary>
public sealed class DeleteTenantCommandHandler(ITenantRepository tenantRepository) : ICommandHandler<DeleteTenantCommand>
{
    public async Task<Unit> Handle(DeleteTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await tenantRepository.DeleteAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
