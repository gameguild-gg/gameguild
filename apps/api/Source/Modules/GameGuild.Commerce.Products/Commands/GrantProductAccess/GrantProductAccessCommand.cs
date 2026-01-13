using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Command to grant a user access to a product
/// </summary>
/// <param name="UserId">User ID</param>
/// <param name="ProductId">Product ID</param>
/// <param name="AcquisitionType">How the user acquired access</param>
/// <param name="PricePaid">Amount paid (if applicable)</param>
/// <param name="Currency">Currency code</param>
/// <param name="AccessEndDate">When access expires (null = permanent)</param>
/// <param name="SubscriptionId">Associated subscription ID (if applicable)</param>
public record GrantProductAccessCommand(
    Guid UserId,
    Guid ProductId,
    ProductAcquisitionType AcquisitionType = ProductAcquisitionType.Grant,
    decimal PricePaid = 0,
    string Currency = "USD",
    DateTime? AccessEndDate = null,
    Guid? SubscriptionId = null
) : ICommand<UserProductAccessDto>;
