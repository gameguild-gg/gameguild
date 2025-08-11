using GameGuild.Common;


namespace GameGuild.Modules.Products;

/// <summary>
/// Interface for user product access services
/// </summary>
public interface IUserProductService {
  Task<UserProduct> GrantProductAccessAsync(
    int userId, int productId, ProductAcquisitionType acquisitionType,
    decimal pricePaid = 0
  );

  Task<UserProduct?> GetUserProductAsync(int userId, int productId);

  Task<IEnumerable<UserProduct>> GetUserProductsAsync(int userId);

  Task<bool> HasProductAccessAsync(int userId, int productId);

  Task<bool> RevokeProductAccessAsync(int userId, int productId, string reason);

  Task<bool> ExtendProductAccessAsync(int userId, int productId, DateTime newEndDate);

  Task<IEnumerable<UserProduct>> GetExpiringAccessAsync(DateTime thresholdDate);
}
