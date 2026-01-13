using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Handler for deleting a promo code
/// </summary>
public class DeletePromoCodeCommandHandler(IPromoCodeRepository promoCodeRepository)
    : ICommandHandler<DeletePromoCodeCommand, Unit>
{
    public async Task<Unit> Handle(DeletePromoCodeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var promoCode = await promoCodeRepository.GetByIdAsync(request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (promoCode == null)
        {
            throw new PromoCodeNotFoundException(request.Id);
        }

        await promoCodeRepository.DeleteAsync(promoCode, cancellationToken).ConfigureAwait(false);
        await promoCodeRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
