using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query handler for getting paged subscription plans
/// </summary>
public class GetPagedSubscriptionPlansQueryHandler(ISubscriptionPlanRepository subscriptionPlanRepository) 
    : IQueryHandler<GetPagedSubscriptionPlansQuery, IEnumerable<SubscriptionPlan>>
{
    public async Task<IEnumerable<SubscriptionPlan>> Handle(GetPagedSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var pagedResult = await subscriptionPlanRepository.GetPagedAsync(
            skip, 
            request.PageSize, 
            request.SearchTerm, 
            includeDeleted: false, 
            cancellationToken).ConfigureAwait(false);
        
        var plans = pagedResult.Items.AsEnumerable();
        
        // Apply additional filters if specified
        if (request.IsActive.HasValue)
        {
            plans = plans.Where(p => !p.IsDeleted == request.IsActive.Value);
        }
        
        if (request.IsFeatured.HasValue)
        {
            plans = plans.Where(p => p.IsFeatured == request.IsFeatured.Value);
        }
        
        return plans;
    }
}
