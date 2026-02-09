using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Handler for getting user products
/// </summary>
public sealed class GetUserProductsQueryHandler(IUserProductRepository userProductRepository)
    : IQueryHandler<GetUserProductsQuery, IReadOnlyList<UserProductAccessDto>>
{
    public async Task<IReadOnlyList<UserProductAccessDto>> Handle(
        GetUserProductsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userProducts = await userProductRepository.GetByUserIdAsync(
            request.UserId,
            request.Status,
            cancellationToken).ConfigureAwait(false);

        return userProducts.Select(up => new UserProductAccessDto(
            up.UserId,
            up.ProductId,
            up.AccessStatus,
            up.AcquisitionType,
            up.PricePaid,
            up.Currency,
            up.AccessStartDate,
            up.AccessEndDate,
            up.CreatedAt
        )).ToList();
    }
}
