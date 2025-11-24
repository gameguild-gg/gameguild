using GameGuild.CQRS;
using GameGuild.Resources.Abstractions;
using GameGuild.Resources.Entities;

namespace GameGuild.Resources.Commands;

/// <summary>
///     Handler for deleting resource quotas
/// </summary>
public class DeleteResourceQuotaCommandHandler(IResourceQuotaRepository resourceQuotaRepository) : ICommandHandler<DeleteResourceQuotaCommand>
{
    public async Task<Unit> Handle(DeleteResourceQuotaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var quota = await resourceQuotaRepository.GetByTenantAndTypeAsync(request.TenantId, request.Type, cancellationToken).ConfigureAwait(false);

        if (quota == null) return Unit.Value;

        await resourceQuotaRepository.DeleteAsync(quota.Id, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
