using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Handler for getting all orders (admin)
/// </summary>
public sealed class GetAllOrdersQueryHandler(
    IApplicationDbContext dbContext)
    : IQueryHandler<GetAllOrdersQuery, IEnumerable<Order>>
{
    public async Task<IEnumerable<Order>> Handle(
        GetAllOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<Order>().AsQueryable();
        if (request.Status.HasValue)
        {
            query = query.Where(o => o.Status == request.Status.Value);
        }

        return await query.OrderByDescending(o => o.CreatedAt).Take(500)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
