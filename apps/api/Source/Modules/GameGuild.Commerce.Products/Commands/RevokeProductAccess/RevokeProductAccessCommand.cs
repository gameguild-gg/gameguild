using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Command to revoke product access from a user
/// </summary>
/// <param name="UserId">User ID</param>
/// <param name="ProductId">Product ID</param>
/// <param name="Reason">Reason for revocation</param>
public record RevokeProductAccessCommand(
    Guid UserId,
    Guid ProductId,
    string? Reason = null
) : ICommand<Unit>;
