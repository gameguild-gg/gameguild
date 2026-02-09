using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace GameGuild.CQRS.Infrastructure;

/// <summary>
///     Compiles <see cref="MethodInfo"/> instances into fast delegates using expression trees.
///     The compiled delegate is ~100× faster than <c>MethodInfo.Invoke</c> after the one-time compilation cost.
///     Shared across the CQRS infrastructure to eliminate code duplication (DRY).
/// </summary>
public static class ExpressionTreeCompiler
{
    private static readonly ConcurrentDictionary<MethodInfo, Func<object, object[], Task<object?>>> Cache = new();
    private static readonly ConcurrentDictionary<Type, Func<Task, object?>> TaskResultAccessorCache = new();

    /// <summary>
    ///     Gets or creates a compiled invoker for the specified method.
    ///     Results are cached for O(1) subsequent lookups.
    /// </summary>
    /// <param name="method">The method to compile into a delegate</param>
    /// <returns>A compiled delegate that invokes the method</returns>
    public static Func<object, object[], Task<object?>> GetOrCompile(MethodInfo method)
    {
        return Cache.GetOrAdd(method, Compile);
    }

    /// <summary>
    ///     Compiles a <see cref="MethodInfo"/> into a fast <c>Func&lt;object, object[], Task&lt;object?&gt;&gt;</c>
    ///     delegate using expression trees.
    /// </summary>
    private static Func<object, object[], Task<object?>> Compile(MethodInfo method)
    {
        // Parameters: (object handler, object[] args)
        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var argsParam = Expression.Parameter(typeof(object[]), "args");

        // Build argument expressions: (ParameterType)args[i]
        var parameters = method.GetParameters();
        var argExpressions = new Expression[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            argExpressions[i] = Expression.Convert(
                Expression.ArrayIndex(argsParam, Expression.Constant(i)),
                parameters[i].ParameterType);
        }

        // Call: ((DeclaringType)handler).Method(args...)
        var call = Expression.Call(
            Expression.Convert(handlerParam, method.DeclaringType!),
            method,
            argExpressions);

        // Box result to object
        var body = Expression.Convert(call, typeof(object));
        var lambda = Expression.Lambda<Func<object, object[], object>>(body, handlerParam, argsParam);
        var compiled = lambda.Compile();

        // Wrap: handle Task extraction so callers always get Task<object?>
        // Uses a cached compiled accessor for Task<T>.Result to avoid per-call reflection.
        return async (handler, args) =>
        {
            var result = compiled(handler, args);
            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                var taskType = task.GetType();
                if (taskType.IsGenericType)
                {
                    var accessor = TaskResultAccessorCache.GetOrAdd(taskType, static tt =>
                    {
                        // Compile: (Task t) => (object?)((Task<T>)t).Result
                        var param = Expression.Parameter(typeof(Task), "t");
                        var cast = Expression.Convert(param, tt);
                        var prop = tt.GetProperty("Result")!;
                        var access = Expression.Property(cast, prop);
                        var boxed = Expression.Convert(access, typeof(object));
                        return Expression.Lambda<Func<Task, object?>>(boxed, param).Compile();
                    });
                    return accessor(task);
                }
                return null;
            }
            return result;
        };
    }
}
