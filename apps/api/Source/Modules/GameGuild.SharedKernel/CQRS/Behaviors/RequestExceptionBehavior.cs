using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using GameGuild.CQRS.Infrastructure;
using Microsoft.Extensions.Logging;

namespace GameGuild.CQRS;

/// <summary>
///     Pipeline behavior for exception handling with optimized reflection caching
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public sealed class RequestExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequestBase
{
    // Cache for compiled handler types and methods - O(1) lookup
    // Static fields in generic types are intentional - each closed generic type gets its own cache
    // ReSharper disable StaticMemberInGenericType
#pragma warning disable CA1000 // Do not declare static members on generic types - Intentional for per-type caching
    private static readonly ConcurrentDictionary<Type, Type> s_handlerTypeCache = new();

    private static readonly ConcurrentDictionary<Type, Type> s_actionTypeCache = new();

    private static readonly ConcurrentDictionary<Type, Type> s_enumerableActionTypeCache = new();

    private static readonly ConcurrentDictionary<Type, MethodInfo?> s_handleMethodCache = new();

    private static readonly ConcurrentDictionary<Type, MethodInfo?> s_executeMethodCache = new();
#pragma warning restore CA1000
    // ReSharper restore StaticMemberInGenericType

    private readonly ILogger<RequestExceptionBehavior<TRequest, TResponse>> _logger;

    private readonly ServiceFactory _serviceFactory;

    /// <summary>
    ///     Initializes a new instance of the RequestExceptionBehavior class
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="serviceFactory">Service factory</param>
    public RequestExceptionBehavior(ILogger<RequestExceptionBehavior<TRequest, TResponse>> logger, ServiceFactory serviceFactory)
    {
        _logger = logger;
        _serviceFactory = serviceFactory;
    }

    /// <summary>
    ///     Handles the pipeline behavior
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="next">Next delegate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response</returns>
    /// <exception cref="Exception">Re-throws if no handler processes the exception</exception>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        try { return await next().ConfigureAwait(false); }
        catch (Exception exception)
        {
            var exceptionType = exception.GetType();

            // Try to find a specific exception handler with O(1) cached lookup
            var handlerType = s_handlerTypeCache.GetOrAdd(exceptionType, static et => typeof(IRequestExceptionHandler<,,>).MakeGenericType(typeof(TRequest), typeof(TResponse), et));

            var exceptionHandler = _serviceFactory(handlerType);

            if (exceptionHandler != null)
            {
                _logger.LogDebug("Found exception handler {HandlerType} for {ExceptionType}", handlerType.Name, exceptionType.Name);

                // Cached method lookup - O(1)
                var method = s_handleMethodCache.GetOrAdd(handlerType, static ht => ht.GetMethod("Handle"));

                if (method != null)
                {
                    var stateWrapper = new RequestExceptionHandlerStateWrapper();
                    var compiledHandle = ExpressionTreeCompiler.GetOrCompile(method);
                    var handleResult = await compiledHandle(exceptionHandler, [request, exception, stateWrapper, cancellationToken]).ConfigureAwait(false);

                    if (stateWrapper.State == RequestExceptionHandlerState.Handled && handleResult is TResponse handlerResult)
                    {
                        return handlerResult;
                    }
                }
            }

            // Try to find exception action handlers with O(1) cached lookup
            var actionType = s_actionTypeCache.GetOrAdd(exceptionType, static et => typeof(IRequestExceptionAction<,>).MakeGenericType(typeof(TRequest), et));

            var enumerableActionType = s_enumerableActionTypeCache.GetOrAdd(actionType, static at => typeof(IEnumerable<>).MakeGenericType(at));

            var actions = _serviceFactory(enumerableActionType) as IEnumerable;

            if (actions != null)
            {
                // Cached method lookup - O(1)
                var executeMethod = s_executeMethodCache.GetOrAdd(actionType, static at => at.GetMethod("Execute"));

                foreach (var action in actions)
                {
                    _logger.LogDebug("Executing exception action {ActionType} for {ExceptionType}", action.GetType().Name, exceptionType.Name);

                    if (executeMethod != null)
                    {
                        var compiledExecute = ExpressionTreeCompiler.GetOrCompile(executeMethod);
                        await compiledExecute(action, [request, exception, cancellationToken]).ConfigureAwait(false);
                    }
                }
            }

            _logger.LogError(exception, "Unhandled exception in request pipeline for {RequestType}", typeof(TRequest).Name);

            throw;
        }
    }

}
