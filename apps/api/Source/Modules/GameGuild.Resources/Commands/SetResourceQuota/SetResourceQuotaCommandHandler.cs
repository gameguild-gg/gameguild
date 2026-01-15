using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Resources;

/// <summary>
///     Handler for setting resource quotas with audit logging for SOC2/ISO 27001 compliance
/// </summary>
public class SetResourceQuotaCommandHandler(
    IResourceQuotaRepository repository,
    IPublisher publisher,
    IActorContextAccessor actorContextAccessor) : ICommandHandler<SetResourceQuotaCommand>
{
    public async Task<Unit> Handle(SetResourceQuotaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var quota = await repository.GetByTenantAndTypeAsync(request.TenantId, request.Type, cancellationToken).ConfigureAwait(false);
        var isNew = quota == null;
        var previousUsage = quota?.CurrentUsage ?? 0;
        var actor = actorContextAccessor.ActorContext;
        var actorId = actor?.SubjectIdAsGuid;

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

        // Publish audit event for SOC2/ISO 27001 compliance
        await publisher.Publish(new QuotaChangedEvent(
            TenantId: request.TenantId,
            ResourceType: request.Type,
            ChangeType: isNew ? QuotaChangeType.Created : QuotaChangeType.LimitsUpdated,
            PreviousUsage: previousUsage,
            CurrentUsage: quota.CurrentUsage,
            SoftLimit: request.SoftLimit,
            HardLimit: request.HardLimit,
            Source: "SetResourceQuotaCommand",
            ActorId: actorId,
            Timestamp: DateTime.UtcNow), cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
