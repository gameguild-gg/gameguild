namespace GameGuild.Modules.Payments.Interfaces;

/// <summary>
/// Interface for payment method management services
/// </summary>
public interface IUserFinancialMethodService {
  Task<Models.UserFinancialMethod> AddPaymentMethodAsync(Models.UserFinancialMethod paymentMethod);

  Task<Models.UserFinancialMethod?> GetPaymentMethodByIdAsync(int id);

  Task<IEnumerable<Models.UserFinancialMethod>> GetUserPaymentMethodsAsync(int userId);

  Task<Models.UserFinancialMethod?> GetDefaultPaymentMethodAsync(int userId);

  Task<bool> SetDefaultPaymentMethodAsync(int userId, int paymentMethodId);

  Task<Models.UserFinancialMethod> UpdatePaymentMethodAsync(Models.UserFinancialMethod paymentMethod);

  Task<bool> RemovePaymentMethodAsync(int id);

  Task<bool> ValidatePaymentMethodAsync(int id);
}
