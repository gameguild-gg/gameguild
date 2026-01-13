using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Handler for checking product access
/// </summary>
public class CheckProductAccessQueryHandler(IUserProductRepository userProductRepository)
    : IQueryHandler<CheckProductAccessQuery, ProductAccessCheckResult>
{
    public async Task<ProductAccessCheckResult> Handle(
        CheckProductAccessQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userProduct = await userProductRepository.GetByUserAndProductAsync(
            request.UserId,
            request.ProductId,
            cancellationToken).ConfigureAwait(false);

        if (userProduct == null)
        {
            return new ProductAccessCheckResult(false);
        }

        var hasAccess = userProduct.AccessStatus == ProductAccessStatus.Active &&
                        (userProduct.AccessEndDate == null || userProduct.AccessEndDate > DateTime.UtcNow);

        return new ProductAccessCheckResult(
            hasAccess,
            userProduct.AccessStatus,
            userProduct.AccessEndDate,
            userProduct.AcquisitionType
        );
    }
}
