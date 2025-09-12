using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace GameGuild;

/// <summary>
///     Specification pattern interface for encapsulating query logic
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    ///     Gets the criteria expression
    /// </summary>
    Expression<Func<T, bool>> Criteria { get; }

    /// <summary>
    ///     Gets the include expressions for eager loading
    /// </summary>
    ReadOnlyCollection<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    ///     Gets the include string expressions for string-based includes
    /// </summary>
    ReadOnlyCollection<string> IncludeStrings { get; }

    /// <summary>
    ///     Gets the order by expression
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    ///     Gets the order by descending expression
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    ///     Gets the group by expression
    /// </summary>
    Expression<Func<T, object>>? GroupBy { get; }

    /// <summary>
    ///     Gets whether to include deleted entities
    /// </summary>
    bool IncludeDeleted { get; }

    /// <summary>
    ///     Gets the number of items to take
    /// </summary>
    int Take { get; }

    /// <summary>
    ///     Gets the number of items to skip
    /// </summary>
    int Skip { get; }

    /// <summary>
    ///     Gets whether paging is enabled
    /// </summary>
    bool IsPagingEnabled { get; }

    /// <summary>
    ///     Gets whether this specification should split queries
    /// </summary>
    bool SplitQuery { get; }

    /// <summary>
    ///     Gets whether this specification should disable change tracking
    /// </summary>
    bool AsNoTracking { get; }

    /// <summary>
    ///     Gets whether this specification should disable identity resolution
    /// </summary>
    bool AsNoTrackingWithIdentityResolution { get; }
}
