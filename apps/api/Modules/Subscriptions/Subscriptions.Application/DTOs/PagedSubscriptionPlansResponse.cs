namespace Subscriptions.Domain.SubscriptionPlans.Models;

/// <summary>
///     Response model for paginated subscription plans
/// </summary>
public class PagedSubscriptionPlansResponse
{
    public IEnumerable<SubscriptionPlanDto> Items { get; set; } = new List<SubscriptionPlanDto>();

    public int TotalCount { get; set; }

    public int PageSize { get; set; }

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }

    public bool HasNextPage { get; set; }

    public bool HasPreviousPage { get; set; }
}

