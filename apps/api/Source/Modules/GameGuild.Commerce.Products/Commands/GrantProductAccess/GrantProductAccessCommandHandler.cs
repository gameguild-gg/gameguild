using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Handler for granting product access to a user
/// </summary>
public class GrantProductAccessCommandHandler(
    IUserProductRepository userProductRepository,
    IProductRepository productRepository)
    : ICommandHandler<GrantProductAccessCommand, UserProductAccessDto>
{
    public async Task<UserProductAccessDto> Handle(
        GrantProductAccessCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Verify product exists
        if (!await productRepository.ExistsAsync(request.ProductId, cancellationToken).ConfigureAwait(false))
        {
            throw new ProductNotFoundException(request.ProductId);
        }

        // Check if user already has access
        var existingAccess = await userProductRepository.GetByUserAndProductAsync(
            request.UserId,
            request.ProductId,
            cancellationToken).ConfigureAwait(false);

        if (existingAccess != null)
        {
            // Update existing access
            existingAccess.AccessStatus = ProductAccessStatus.Active;
            existingAccess.AcquisitionType = request.AcquisitionType;
            existingAccess.PricePaid = request.PricePaid;
            existingAccess.Currency = request.Currency;
            existingAccess.AccessEndDate = request.AccessEndDate;
            existingAccess.SubscriptionId = request.SubscriptionId;
            existingAccess.UpdatedAt = DateTime.UtcNow;

            await userProductRepository.UpdateAsync(existingAccess, cancellationToken).ConfigureAwait(false);
            await userProductRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return MapToDto(existingAccess);
        }

        // Create new access
        var userProduct = new UserProduct
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            ProductId = request.ProductId,
            AcquisitionType = request.AcquisitionType,
            AccessStatus = ProductAccessStatus.Active,
            PricePaid = request.PricePaid,
            Currency = request.Currency,
            AccessEndDate = request.AccessEndDate,
            SubscriptionId = request.SubscriptionId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await userProductRepository.AddAsync(userProduct, cancellationToken).ConfigureAwait(false);
        await userProductRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return MapToDto(userProduct);
    }

    private static UserProductAccessDto MapToDto(UserProduct userProduct)
    {
        return new UserProductAccessDto(
            userProduct.UserId,
            userProduct.ProductId,
            userProduct.AccessStatus,
            userProduct.AcquisitionType,
            userProduct.PricePaid,
            userProduct.Currency,
            userProduct.CreatedAt,
            userProduct.AccessEndDate,
            userProduct.CreatedAt
        );
    }
}
