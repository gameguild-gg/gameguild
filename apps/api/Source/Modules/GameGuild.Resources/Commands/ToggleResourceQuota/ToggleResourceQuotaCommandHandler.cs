using GameGuild.CQRS;
using GameGuild.Resources.Abstractions;
using GameGuild.Resources.Entities;

namespace GameGuild.Resources.Commands;

/// <summary>
///     Handler for toggling resource quota activation status
/// </summary>
public class ToggleResourceQuotaCommandHandler(IResourceQuotaRepository resourceQuotaRepository) : ICommandHandler<ToggleResourceQuotaCommand>
{
    public async Task<Unit> Handle(ToggleResourceQuotaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var quota = await resourceQuotaRepository.GetByTenantAndTypeAsync(request.TenantId, request.Type, cancellationToken).ConfigureAwait(false);

        if (quota == null) return Unit.Value;

        quota.IsActive = request.IsActive;
        quota.UpdatedAt = DateTime.UtcNow;

        await resourceQuotaRepository.UpdateAsync(quota, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
