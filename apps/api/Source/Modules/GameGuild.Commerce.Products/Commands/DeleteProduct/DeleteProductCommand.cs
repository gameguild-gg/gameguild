using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Command to delete a product (soft delete by default)
/// </summary>
/// <param name="ProductId">ID of the product to delete</param>
/// <param name="SoftDelete">Whether to soft delete (default) or hard delete</param>
/// <param name="Reason">Optional reason for deletion</param>
/// <param name="ExpectedVersion">Expected version for optimistic concurrency</param>
public record DeleteProductCommand(
    Guid ProductId,
    bool SoftDelete = true,
    string? Reason = null,
    long? ExpectedVersion = null
) : ICommand;
