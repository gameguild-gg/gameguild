using GameGuild.CQRS;
using GameGuild.Resources.Abstractions;
using GameGuild.Resources.Entities;

namespace GameGuild.Resources.Commands;

/// <summary>
///     Handler for resetting resource quotas
/// </summary>
public class ResetResourceQuotaCommandHandler(IResourceQuotaRepository resourceQuotaRepository) : ICommandHandler<ResetResourceQuotaCommand>
{
    public async Task<Unit> Handle(ResetResourceQuotaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var quota = await resourceQuotaRepository.GetByTenantAndTypeAsync(request.TenantId, request.Type, cancellationToken).ConfigureAwait(false);

        if (quota == null) return Unit.Value;

        quota.ResetUsage();
        quota.UpdatedAt = DateTime.UtcNow;

        await resourceQuotaRepository.UpdateAsync(quota, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
