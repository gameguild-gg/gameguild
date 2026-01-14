using GameGuild.Identity.Users;

namespace GameGuild.Commerce.Products;

/// <summary>
///     Interface representing a product creator with minimal required properties.
///     This abstraction reduces coupling between Products module and Identity module
///     by exposing only the creator information needed by products.
/// </summary>
/// <remarks>
///     <para>
///         This interface defines the minimal contract for creator information.
///         The Products module only needs basic creator details (Id, Name, Email)
///         rather than the full User entity.
///     </para>
///     <para>
///         The <see cref="User"/> class implements this interface, allowing
///         Products module code to work with creators through the abstraction
///         when full User functionality isn't needed.
///     </para>
/// </remarks>
public interface ICreator
{
    /// <summary>
    ///     Unique identifier of the creator
    /// </summary>
    Guid Id { get; }

    /// <summary>
    ///     Display name of the creator
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Email address of the creator
    /// </summary>
    string Email { get; }

    /// <summary>
    ///     Whether the creator account is active
    /// </summary>
    bool IsActive { get; }
}

/// <summary>
///     DTO representing creator information extracted from a User entity.
///     Use this when you only need creator info without the full User entity.
/// </summary>
public sealed record CreatorInfo(Guid Id, string Name, string Email, bool IsActive) : ICreator;

/// <summary>
///     Extension methods for projecting User entities to creator abstractions
/// </summary>
public static class CreatorExtensions
{
    /// <summary>
    ///     Converts a User entity to a CreatorInfo DTO
    /// </summary>
    /// <param name="user">The user to convert</param>
    /// <returns>A CreatorInfo containing minimal creator data</returns>
    public static CreatorInfo ToCreatorInfo(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new CreatorInfo(user.Id, user.Name, user.Email, user.IsActive);
    }

    /// <summary>
    ///     Converts an IUser interface to a CreatorInfo DTO
    /// </summary>
    /// <param name="user">The user interface to convert</param>
    /// <returns>A CreatorInfo containing minimal creator data</returns>
    public static CreatorInfo ToCreatorInfo(this IUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new CreatorInfo(user.Id, user.Name, user.Email, user.IsActive);
    }
}

