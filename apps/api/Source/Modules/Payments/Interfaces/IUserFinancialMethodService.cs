namespace GameGuild.Modules.Payments;

/// <summary>
/// Interface for payment method management services
/// </summary>
public interface IUserFinancialMethodService {
  Task<UserFinancialMethod> AddPaymentMethodAsync(UserFinancialMethod paymentMethod);

  Task<UserFinancialMethod?> GetPaymentMethodByIdAsync(int id);

  Task<IEnumerable<UserFinancialMethod>> GetUserPaymentMethodsAsync(int userId);

  Task<UserFinancialMethod?> GetDefaultPaymentMethodAsync(int userId);

  Task<bool> SetDefaultPaymentMethodAsync(int userId, int paymentMethodId);

  Task<UserFinancialMethod> UpdatePaymentMethodAsync(UserFinancialMethod paymentMethod);

  Task<bool> RemovePaymentMethodAsync(int id);

  Task<bool> ValidatePaymentMethodAsync(int id);
}
