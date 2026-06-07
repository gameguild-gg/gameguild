namespace GameGuild.Commerce.Products;

/// <summary> Interface for promotional code services </summary>
public interface IPromoCodeService {
  Task<PromoCode> CreatePromoCodeAsync(PromoCode promoCode);

  Task<PromoCode?> GetPromoCodeByCodeAsync(string code);

  Task<bool> ValidatePromoCodeAsync(string code, Guid userId, Guid? productId = null);

  Task<decimal> CalculateDiscountAsync(string code, decimal originalAmount);

  Task<PromoCodeUse> ApplyPromoCodeAsync(string code, Guid userId, Guid transactionId);

  Task<bool> DeactivatePromoCodeAsync(Guid id);

  Task<IEnumerable<PromoCode>> GetActivePromoCodesAsync();
}
