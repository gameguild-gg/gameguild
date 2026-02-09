using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command handler for creating a new subscription plan
/// </summary>
public sealed class CreateSubscriptionPlanCommandHandler(
    ISubscriptionPlanRepository subscriptionPlanRepository,
    ILogger<CreateSubscriptionPlanCommandHandler> logger) : ICommandHandler<CreateSubscriptionPlanCommand, Guid>
{
    public async Task<Guid> Handle(CreateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = new SubscriptionPlan(
            request.Name,
            request.Slug,
            request.MonthlyPriceInCents,
            request.Currency,
            request.Description);

        await subscriptionPlanRepository.AddAsync(plan, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Created subscription plan {PlanId} with name {PlanName} and slug {PlanSlug}",
            plan.Id, plan.Name, plan.Slug);

        return plan.Id;
    }
}
