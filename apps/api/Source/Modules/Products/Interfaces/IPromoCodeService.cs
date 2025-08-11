namespace GameGuild.Modules.Products;

/// <summary>
/// Interface for promotional code services
/// </summary>
public interface IPromoCodeService {
  Task<PromoCode> CreatePromoCodeAsync(PromoCode promoCode);

  Task<PromoCode?> GetPromoCodeByCodeAsync(string code);

  Task<bool> ValidatePromoCodeAsync(string code, int userId, int? productId = null);

  Task<decimal> CalculateDiscountAsync(string code, decimal originalAmount);

  Task<PromoCodeUse> ApplyPromoCodeAsync(string code, int userId, int transactionId);

  Task<bool> DeactivatePromoCodeAsync(int id);

  Task<IEnumerable<PromoCode>> GetActivePromoCodesAsync();
}
