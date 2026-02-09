using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Query to get all products a user has access to
/// </summary>
/// <param name="UserId">User ID</param>
/// <param name="Status">Optional status filter</param>
public sealed record GetUserProductsQuery(
    Guid UserId,
    ProductAccessStatus? Status = null
) : IQuery<IReadOnlyList<UserProductAccessDto>>;
