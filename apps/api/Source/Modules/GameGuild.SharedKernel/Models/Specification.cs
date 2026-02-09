using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace GameGuild;

/// <summary>
///     Base specification class implementing the Specification pattern for encapsulating query logic.
///     All specification classes in the solution should inherit from this single base class.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public abstract class Specification<T>() : ISpecification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = new();

    private readonly List<string> _includeStrings = new();

    /// <summary>
    ///     Initializes a new instance with a criteria expression
    /// </summary>
    /// <param name="criteria">The criteria expression</param>
    protected Specification(Expression<Func<T, bool>> criteria) : this() { Criteria = criteria; }

    /// <inheritdoc />
    public Expression<Func<T, bool>>? Criteria { get; private set; }

    /// <inheritdoc />
    public ReadOnlyCollection<Expression<Func<T, object>>> Includes { get => _includes.AsReadOnly(); }

    /// <inheritdoc />
    public ReadOnlyCollection<string> IncludeStrings { get => _includeStrings.AsReadOnly(); }

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderBy { get; private set; }

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    /// <inheritdoc />
    public Expression<Func<T, object>>? GroupBy { get; private set; }

    /// <inheritdoc />
    public bool IncludeDeleted { get; private set; }

    /// <inheritdoc />
    public int Take { get; private set; }

    /// <inheritdoc />
    public int Skip { get; private set; }

    /// <inheritdoc />
    public bool IsPagingEnabled { get; private set; }

    /// <inheritdoc />
    public bool SplitQuery { get; private set; }

    /// <inheritdoc />
    public bool AsNoTracking { get; private set; }

    /// <inheritdoc />
    public bool AsNoTrackingWithIdentityResolution { get; private set; }

    /// <summary>
    ///     Adds an include expression
    /// </summary>
    /// <param name="includeExpression">The include expression</param>
    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression) { _includes.Add(includeExpression); }

    /// <summary>
    ///     Adds a string-based include
    /// </summary>
    /// <param name="includeString">The include string</param>
    protected virtual void AddInclude(string includeString) { _includeStrings.Add(includeString); }

    /// <summary>
    ///     Applies ordering
    /// </summary>
    /// <param name="orderByExpression">The order by expression</param>
    protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression) { OrderBy = orderByExpression; }

    /// <summary>
    ///     Applies descending ordering
    /// </summary>
    /// <param name="orderByDescendingExpression">The order by descending expression</param>
    protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression) { OrderByDescending = orderByDescendingExpression; }

    /// <summary>
    ///     Applies grouping
    /// </summary>
    /// <param name="groupByExpression">The group by expression</param>
    protected virtual void ApplyGroupBy(Expression<Func<T, object>> groupByExpression) { GroupBy = groupByExpression; }

    /// <summary>
    ///     Applies paging
    /// </summary>
    /// <param name="skip">The number of items to skip</param>
    /// <param name="take">The number of items to take</param>
    protected virtual void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    /// <summary>
    ///     Applies the criteria expression
    /// </summary>
    /// <param name="criteria">The criteria expression</param>
    protected virtual void ApplyCriteria(Expression<Func<T, bool>> criteria) { Criteria = criteria; }

    /// <summary>
    ///     Enables including soft-deleted entities in query results
    /// </summary>
    protected virtual void IncludeDeletedEntities() { IncludeDeleted = true; }

    /// <summary>
    ///     Enables split query execution for large includes
    /// </summary>
    protected virtual void EnableSplitQuery() { SplitQuery = true; }

    /// <summary>
    ///     Disables change tracking for read-only queries
    /// </summary>
    protected virtual void EnableAsNoTracking() { AsNoTracking = true; }

    /// <summary>
    ///     Disables change tracking with identity resolution
    /// </summary>
    protected virtual void EnableAsNoTrackingWithIdentityResolution() { AsNoTrackingWithIdentityResolution = true; }
}
