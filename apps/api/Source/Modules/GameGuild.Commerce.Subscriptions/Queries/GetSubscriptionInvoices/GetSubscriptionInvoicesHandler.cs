using GameGuild.Abstractions;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for GetSubscriptionInvoicesQuery
/// </summary>
public sealed class GetSubscriptionInvoicesHandler(IApplicationDbContext context)
    : IQueryHandler<GetSubscriptionInvoicesQuery, GameGuild.CQRS.PagedResult<SubscriptionInvoiceDto>>
{
    public async Task<GameGuild.CQRS.PagedResult<SubscriptionInvoiceDto>> Handle(
        GetSubscriptionInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        // Check if subscription exists
        var subscriptionExists = await context.Set<Subscription>()
            .AnyAsync(s => s.Id == request.SubscriptionId, cancellationToken);

        if (!subscriptionExists)
        {
            return new GameGuild.CQRS.PagedResult<SubscriptionInvoiceDto>([], 0, (request.Page - 1) * request.PageSize, request.PageSize);
        }

        // Note: This assumes there's an Invoice entity related to subscriptions.
        // If not, this would need to be adjusted to work with the actual data model.
        // For now, return an empty result as a placeholder.
        var items = new List<SubscriptionInvoiceDto>();
        var totalCount = 0;

        return new GameGuild.CQRS.PagedResult<SubscriptionInvoiceDto>(
            items,
            totalCount,
            (request.Page - 1) * request.PageSize,
            request.PageSize);
    }
}
