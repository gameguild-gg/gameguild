namespace GameGuild.Modules.Products.Interfaces;

/// <summary>
/// Interface for promotional code services
/// </summary>
public interface IPromoCodeService {
  Task<Models.PromoCode> CreatePromoCodeAsync(Models.PromoCode promoCode);

  Task<Models.PromoCode?> GetPromoCodeByCodeAsync(string code);

  Task<bool> ValidatePromoCodeAsync(string code, int userId, int? productId = null);

  Task<decimal> CalculateDiscountAsync(string code, decimal originalAmount);

  Task<Models.PromoCodeUse> ApplyPromoCodeAsync(string code, int userId, int transactionId);

  Task<bool> DeactivatePromoCodeAsync(int id);

  Task<IEnumerable<Models.PromoCode>> GetActivePromoCodesAsync();
}
