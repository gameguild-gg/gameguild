using System.Collections.ObjectModel;
using System.Linq.Expressions;


namespace GameGuild;

/// <summary> Base implementation of the specification pattern </summary>
/// <typeparam name="T"> The entity type </typeparam>
public abstract class SpecificationBase<T> : ISpecification<T> {
  private readonly List<Expression<Func<T, object>>> _includes = new List<Expression<Func<T, object>>>();

  private readonly List<string> _includeStrings = new List<string>();

  /// <summary> Initializes a new instance of the <see cref="SpecificationBase{T}" /> class </summary>
  /// <param name="criteria"> The criteria expression </param>
  protected SpecificationBase(Expression<Func<T, bool>>? criteria = null) { Criteria = criteria; }

  /// <inheritdoc />
  public Expression<Func<T, bool>>? Criteria { get; }

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

  /// <summary> Adds an include expression </summary>
  /// <param name="includeExpression"> The include expression </param>
  protected virtual void AddInclude(Expression<Func<T, object>> includeExpression) { _includes.Add(includeExpression); }

  /// <summary> Adds a string-based include </summary>
  /// <param name="includeString"> The include string </param>
  protected virtual void AddInclude(string includeString) { _includeStrings.Add(includeString); }

  /// <summary> Applies ordering </summary>
  /// <param name="orderByExpression"> The order by expression </param>
  protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression) { OrderBy = orderByExpression; }

  /// <summary> Applies descending ordering </summary>
  /// <param name="orderByDescendingExpression"> The order by descending expression </param>
  protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression) { OrderByDescending = orderByDescendingExpression; }

  /// <summary> Applies grouping </summary>
  /// <param name="groupByExpression"> The group by expression </param>
  protected virtual void ApplyGroupBy(Expression<Func<T, object>> groupByExpression) { GroupBy = groupByExpression; }

  /// <summary> Applies paging </summary>
  /// <param name="skip"> The number of items to skip </param>
  /// <param name="take"> The number of items to take </param>
  protected virtual void ApplyPaging(int skip, int take) {
    Skip = skip;
    Take = take;
    IsPagingEnabled = true;
  }

  /// <summary> Enables including deleted entities </summary>
  protected virtual void IncludeDeletedEntities() { IncludeDeleted = true; }

  /// <summary> Enables split query </summary>
  protected virtual void EnableSplitQuery() { SplitQuery = true; }

  /// <summary> Disables change tracking </summary>
  protected virtual void EnableAsNoTracking() { AsNoTracking = true; }

  /// <summary> Disables identity resolution </summary>
  protected virtual void EnableAsNoTrackingWithIdentityResolution() { AsNoTrackingWithIdentityResolution = true; }
}
