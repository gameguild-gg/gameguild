using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Handler for revoking product access from a user
/// </summary>
public class RevokeProductAccessCommandHandler(IUserProductRepository userProductRepository)
    : ICommandHandler<RevokeProductAccessCommand, Unit>
{
    public async Task<Unit> Handle(RevokeProductAccessCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userProduct = await userProductRepository.GetByUserAndProductAsync(
            request.UserId,
            request.ProductId,
            cancellationToken).ConfigureAwait(false);

        if (userProduct == null)
        {
            throw new InvalidOperationException($"User {request.UserId} does not have access to product {request.ProductId}");
        }

        userProduct.AccessStatus = ProductAccessStatus.Revoked;
        userProduct.Touch();

        await userProductRepository.UpdateAsync(userProduct, cancellationToken).ConfigureAwait(false);
        await userProductRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
