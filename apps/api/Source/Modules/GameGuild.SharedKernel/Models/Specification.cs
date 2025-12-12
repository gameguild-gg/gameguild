using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace GameGuild.Abstractions;

/// <summary>
///     Base specification class implementing common specification functionality
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public abstract class Specification<T>() : ISpecification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = new List<Expression<Func<T, object>>>();

    private readonly List<string> _includeStrings = new List<string>();

    protected Specification(Expression<Func<T, bool>> criteria) : this() { Criteria = criteria; }

    public Expression<Func<T, bool>> Criteria { get; private set; } = null!;

    public ReadOnlyCollection<Expression<Func<T, object>>> Includes { get => _includes.AsReadOnly(); }

    public ReadOnlyCollection<string> IncludeStrings { get => _includeStrings.AsReadOnly(); }

    public Expression<Func<T, object>>? OrderBy { get; private set; }

    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    public Expression<Func<T, object>>? GroupBy { get; private set; }

    public bool IncludeDeleted { get; private set; }

    public int Take { get; private set; }

    public int Skip { get; private set; }

    public bool IsPagingEnabled { get; private set; }

    public bool SplitQuery { get; private set; }

    public bool AsNoTracking { get; private set; }

    public bool AsNoTrackingWithIdentityResolution { get; private set; }

    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression) { _includes.Add(includeExpression); }

    protected virtual void AddInclude(string includeString) { _includeStrings.Add(includeString); }

    protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression) { OrderBy = orderByExpression; }

    protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression) { OrderByDescending = orderByDescendingExpression; }

    protected virtual void ApplyGroupBy(Expression<Func<T, object>> groupByExpression) { GroupBy = groupByExpression; }

    protected virtual void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    protected virtual void ApplyIncludeDeleted() { IncludeDeleted = true; }

    protected virtual void ApplySplitQuery() { SplitQuery = true; }

    protected virtual void ApplyNoTracking() { AsNoTracking = true; }

    protected virtual void ApplyNoTrackingWithIdentityResolution() { AsNoTrackingWithIdentityResolution = true; }

    protected virtual void ApplyCriteria(Expression<Func<T, bool>> criteria) { Criteria = criteria; }
}
