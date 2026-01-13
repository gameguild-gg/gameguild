using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Command handler for deleting a product
/// </summary>
public class DeleteProductCommandHandler(IProductRepository productRepository)
    : ICommandHandler<DeleteProductCommand>
{
    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Get existing product
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false)
            ?? throw new ProductNotFoundException(request.ProductId);

        // Check version for optimistic concurrency
        if (request.ExpectedVersion.HasValue && product.Version != request.ExpectedVersion.Value)
        {
            throw new ConcurrencyException($"Product {request.ProductId} has been modified by another user.");
        }

        if (request.SoftDelete)
        {
            // Soft delete
            product.SoftDelete();
            await productRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Hard delete
            await productRepository.DeleteAsync(product, cancellationToken).ConfigureAwait(false);
            await productRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
