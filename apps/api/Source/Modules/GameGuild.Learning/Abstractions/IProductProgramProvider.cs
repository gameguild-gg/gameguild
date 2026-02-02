namespace GameGuild.Learning.Abstractions;

/// <summary>
/// Interface for accessing product-program relationships.
/// Decouples the Learning module from direct Commerce/Products module dependencies.
/// </summary>
/// <remarks>
/// This interface addresses the DIP violation where ProgramEnrollmentService directly 
/// accessed ProductProgram entity from the Commerce module.
/// </remarks>
public interface IProductProgramProvider
{
    /// <summary>
    /// Gets all program IDs associated with a product, ordered by sort order
    /// </summary>
    /// <param name="productId">The product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ordered list of program IDs included in the product</returns>
    Task<IReadOnlyList<Guid>> GetProgramIdsForProductAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a product includes a specific program
    /// </summary>
    /// <param name="productId">The product ID</param>
    /// <param name="programId">The program ID to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the product includes the program</returns>
    Task<bool> ProductIncludesProgramAsync(Guid productId, Guid programId, CancellationToken cancellationToken = default);
}
