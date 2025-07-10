using GameGuild.Common;


namespace GameGuild.Modules.Products.Interfaces;

/// <summary>
/// Interface for user product access services
/// </summary>
public interface IUserProductService {
  Task<Models.UserProduct> GrantProductAccessAsync(
    int userId, int productId, ProductAcquisitionType acquisitionType,
    decimal pricePaid = 0
  );

  Task<Models.UserProduct?> GetUserProductAsync(int userId, int productId);

  Task<IEnumerable<Models.UserProduct>> GetUserProductsAsync(int userId);

  Task<bool> HasProductAccessAsync(int userId, int productId);

  Task<bool> RevokeProductAccessAsync(int userId, int productId, string reason);

  Task<bool> ExtendProductAccessAsync(int userId, int productId, DateTime newEndDate);

  Task<IEnumerable<Models.UserProduct>> GetExpiringAccessAsync(DateTime thresholdDate);
}
