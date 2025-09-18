using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using GameGuild.CQRS;


namespace GameGuild;

/// <summary> Pipeline behavior for exception handling with optimized reflection caching </summary>
/// <typeparam name="TRequest"> Request type </typeparam>
/// <typeparam name="TResponse"> Response type </typeparam>
public class RequestExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IBaseRequest {
  // Cache for compiled handler types and methods - O(1) lookup
  private static readonly ConcurrentDictionary<Type, Type> HandlerTypeCache = new ConcurrentDictionary<Type, Type>();

  private static readonly ConcurrentDictionary<Type, Type> ActionTypeCache = new ConcurrentDictionary<Type, Type>();

  private static readonly ConcurrentDictionary<Type, Type> EnumerableActionTypeCache = new ConcurrentDictionary<Type, Type>();

  private static readonly ConcurrentDictionary<Type, MethodInfo?> HandleMethodCache = new ConcurrentDictionary<Type, MethodInfo?>();

  private static readonly ConcurrentDictionary<Type, MethodInfo?> ExecuteMethodCache = new ConcurrentDictionary<Type, MethodInfo?>();

  private readonly ILogger<RequestExceptionBehavior<TRequest, TResponse>> _logger;

  private readonly ServiceFactory _serviceFactory;

  /// <summary> Initializes a new instance of the RequestExceptionBehavior class </summary>
  /// <param name="logger"> Logger </param>
  /// <param name="serviceFactory"> Service factory </param>
  public RequestExceptionBehavior(ILogger<RequestExceptionBehavior<TRequest, TResponse>> logger, ServiceFactory serviceFactory) {
    _logger = logger;
    _serviceFactory = serviceFactory;
  }

  /// <summary> Handles the pipeline behavior </summary>
  /// <param name="request"> Request </param>
  /// <param name="next"> Next delegate </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> Response </returns>
  /// <exception cref="Exception"> Re-throws if no handler processes the exception </exception>
  public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegateBase<TResponse> next, CancellationToken cancellationToken) {
    ArgumentNullException.ThrowIfNull(next);

    try { return await next().ConfigureAwait(false); }
    catch (Exception exception) {
      var exceptionType = exception.GetType();

      // Try to find a specific exception handler with O(1) cached lookup
      var handlerType = HandlerTypeCache.GetOrAdd(exceptionType, static et => typeof(IRequestExceptionHandler<,,>).MakeGenericType(typeof(TRequest), typeof(TResponse), et));

      var exceptionHandler = _serviceFactory(handlerType);

      if (exceptionHandler != null) {
        _logger.LogDebug("Found exception handler {HandlerType} for {ExceptionType}", handlerType.Name, exceptionType.Name);

        // Cached method lookup - O(1)
        var method = HandleMethodCache.GetOrAdd(handlerType, static ht => ht.GetMethod("Handle"));

        if (method != null) {
          var result = method.Invoke(exceptionHandler, [request, exception, RequestExceptionHandlerState.Continue, cancellationToken]);

          if (result is Task<TResponse> handlerTask) { return await handlerTask.ConfigureAwait(false); }
        }
      }

      // Try to find exception action handlers with O(1) cached lookup
      var actionType = ActionTypeCache.GetOrAdd(exceptionType, static et => typeof(IRequestExceptionAction<,>).MakeGenericType(typeof(TRequest), et));

      var enumerableActionType = EnumerableActionTypeCache.GetOrAdd(actionType, static at => typeof(IEnumerable<>).MakeGenericType(at));

      var actions = _serviceFactory(enumerableActionType) as IEnumerable;

      if (actions != null) {
        // Cached method lookup - O(1)
        var executeMethod = ExecuteMethodCache.GetOrAdd(actionType, static at => at.GetMethod("Execute"));

        foreach (var action in actions) {
          _logger.LogDebug("Executing exception action {ActionType} for {ExceptionType}", action.GetType().Name, exceptionType.Name);

          if (executeMethod != null) {
            var result = executeMethod.Invoke(action, [request, exception, cancellationToken]);

            if (result is Task actionTask) { await actionTask.ConfigureAwait(false); }
          }
        }
      }

      _logger.LogError(exception, "Unhandled exception in request pipeline for {RequestType}", typeof(TRequest).Name);

      throw new InvalidOperationException($"Unhandled exception in request pipeline for {typeof(TRequest).Name}", exception);
    }
  }
}
