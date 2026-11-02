using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for <see cref="ValidateSubscriptionPlanLimitsQuery" /> (issue #1205 G-8):
///     validates the stated usage requirements against the plan's configured limits
///     (a null limit means unlimited). Returns <see langword="false" /> when the plan does
///     not exist — an unknown plan can never validate.
/// </summary>
public sealed class ValidateSubscriptionPlanLimitsHandler(ISubscriptionPlanRepository planRepository)
    : IQueryHandler<ValidateSubscriptionPlanLimitsQuery, bool>
{
    public async Task<bool> Handle(
        ValidateSubscriptionPlanLimitsQuery request,
        CancellationToken cancellationToken)
    {
        var plan = await planRepository
            .GetByIdAsync(request.PlanId, cancellationToken)
            .ConfigureAwait(false);
        if (plan is null)
        {
            return false;
        }

        return plan.AllowsUserCount(request.Users) &&
               plan.AllowsStorage(request.StorageMb) &&
               plan.AllowsApiCalls(request.ApiCalls);
    }
}
