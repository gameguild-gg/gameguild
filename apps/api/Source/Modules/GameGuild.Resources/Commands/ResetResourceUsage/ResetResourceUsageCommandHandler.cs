using GameGuild.CQRS;
using GameGuild.Resources.Abstractions;

namespace GameGuild.Resources.Commands;

/// <summary>
///     Handler for resetting resource usage records
/// </summary>
public class ResetResourceUsageCommandHandler(IUsageRecordRepository usageRecordRepository) : ICommandHandler<ResetResourceUsageCommand>
{
    public async Task<Unit> Handle(ResetResourceUsageCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ResourceUsageType.HasValue)
        {
            // Reset usage for specific type
            await usageRecordRepository.DeleteByTenantAndTypeAsync(request.TenantId, request.ResourceUsageType.Value, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Reset all usage for tenant
            await usageRecordRepository.DeleteByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
