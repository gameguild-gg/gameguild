using System.Reflection;
using GameGuild.CQRS;
using GameGuild.Resources.Abstractions;
using GameGuild.Resources.Entities;

namespace GameGuild.Resources.Commands;

/// <summary>
///     Handler for setting resource quotas
/// </summary>
public class SetResourceQuotaCommandHandler(IResourceQuotaRepository repository) : ICommandHandler<SetResourceQuotaCommand>
{
    public async Task<Unit> Handle(SetResourceQuotaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var quota = await repository.GetByTenantAndTypeAsync(request.TenantId, request.Type, cancellationToken).ConfigureAwait(false);

        if (quota == null)
        {
            quota = new ResourceQuota
            {
                Id = Guid.NewGuid(),
                Type = request.Type,
                CreatedAt = DateTime.UtcNow,
                SoftLimit = request.SoftLimit,
                HardLimit = request.HardLimit,
                Period = request.Period,
                IsActive = request.IsActive,
                ResetTime = request.ResetTime
            };

            // Set TenantId using reflection since the setter is protected
            var tenantIdProperty = typeof(ResourceQuota).GetProperty("TenantId");
            tenantIdProperty?.GetSetMethod(nonPublic: true)?.Invoke(quota, new object[] { request.TenantId });

            await repository.CreateAsync(quota, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            quota.SoftLimit = request.SoftLimit;
            quota.HardLimit = request.HardLimit;
            quota.Period = request.Period;
            quota.IsActive = request.IsActive;
            quota.ResetTime = request.ResetTime;
            quota.UpdatedAt = DateTime.UtcNow;

            await repository.UpdateAsync(quota, cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
