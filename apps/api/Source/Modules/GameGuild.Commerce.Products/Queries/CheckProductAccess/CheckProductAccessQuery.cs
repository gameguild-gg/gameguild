using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Query to check if a user has access to a product
/// </summary>
/// <param name="UserId">User ID</param>
/// <param name="ProductId">Product ID</param>
public record CheckProductAccessQuery(
    Guid UserId,
    Guid ProductId
) : IQuery<ProductAccessCheckResult>;

/// <summary>
/// Result of checking product access
/// </summary>
/// <param name="HasAccess">Whether the user has active access</param>
/// <param name="AccessStatus">Current access status (if any)</param>
/// <param name="AccessEndDate">When access expires (if applicable)</param>
/// <param name="AcquisitionType">How the user acquired access</param>
public record ProductAccessCheckResult(
    bool HasAccess,
    ProductAccessStatus? AccessStatus = null,
    DateTime? AccessEndDate = null,
    ProductAcquisitionType? AcquisitionType = null
);
