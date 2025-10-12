using System.Linq.Expressions;

namespace GameGuild.Modules.Permissions.Specifications;

/// <summary>
/// Base specification pattern implementation for permission queries
/// </summary>
public abstract class Specification<T>
{
    /// <summary>
    /// Convert specification to expression tree
    /// </summary>
    public abstract Expression<Func<T, bool>> ToExpression();

    /// <summary>
    /// Check if entity satisfies the specification
    /// </summary>
    public bool IsSatisfiedBy(T entity)
    {
        var predicate = ToExpression().Compile();
        return predicate(entity);
    }

    /// <summary>
    /// Combine specifications with AND logic
    /// </summary>
    public Specification<T> And(Specification<T> specification)
    {
        return new AndSpecification<T>(this, specification);
    }

    /// <summary>
    /// Combine specifications with OR logic
    /// </summary>
    public Specification<T> Or(Specification<T> specification)
    {
        return new OrSpecification<T>(this, specification);
    }

    /// <summary>
    /// Negate the specification with NOT logic
    /// </summary>
    public Specification<T> Not()
    {
        return new NotSpecification<T>(this);
    }

    /// <summary>
    /// Implicit conversion from specification to expression
    /// </summary>
    public static implicit operator Expression<Func<T, bool>>(Specification<T> specification)
    {
        return specification.ToExpression();
    }
}

/// <summary>
/// AND specification combiner
/// </summary>
internal class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpression = _left.ToExpression();
        var rightExpression = _right.ToExpression();

        var parameter = Expression.Parameter(typeof(T));
        var leftInvoke = Expression.Invoke(leftExpression, parameter);
        var rightInvoke = Expression.Invoke(rightExpression, parameter);

        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(leftInvoke, rightInvoke), parameter);
    }
}

/// <summary>
/// OR specification combiner
/// </summary>
internal class OrSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public OrSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpression = _left.ToExpression();
        var rightExpression = _right.ToExpression();

        var parameter = Expression.Parameter(typeof(T));
        var leftInvoke = Expression.Invoke(leftExpression, parameter);
        var rightInvoke = Expression.Invoke(rightExpression, parameter);

        return Expression.Lambda<Func<T, bool>>(
            Expression.OrElse(leftInvoke, rightInvoke), parameter);
    }
}

/// <summary>
/// NOT specification negator
/// </summary>
internal class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _specification;

    public NotSpecification(Specification<T> specification)
    {
        _specification = specification;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var expression = _specification.ToExpression();
        var parameter = Expression.Parameter(typeof(T));
        var invoke = Expression.Invoke(expression, parameter);

        return Expression.Lambda<Func<T, bool>>(
            Expression.Not(invoke), parameter);
    }
}