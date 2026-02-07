using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using GameGuild.CQRS.Publishers;

namespace GameGuild.CQRS.Implementation;

/// <summary>
///     Optimized mediator implementation with O(1) handler lookup, compiled delegates, and pipeline behavior support
/// </summary>
public class Mediator : IMediator
{
    // O(1) lookup caches for performance
    // ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles - Static readonly fields intentionally use camelCase for cache fields
    private static readonly ConcurrentDictionary<Type, HandlerMetadata> s_handlerCache = new();

    private static readonly ConcurrentDictionary<Type, Type> s_handlerTypeCache = new();

    private static readonly ConcurrentDictionary<Type, Type> s_pipelineBehaviorTypeCache = new();
#pragma warning restore IDE1006
    // ReSharper restore InconsistentNaming

    private readonly INotificationPublisher _notificationPublisher;

    private readonly ServiceFactory _serviceFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Mediator" /> class.
    /// </summary>
    /// <param name="serviceFactory">Service factory</param>
    /// <param name="notificationPublisher">Notification publisher</param>
    public Mediator(ServiceFactory serviceFactory, INotificationPublisher? notificationPublisher = null)
    {
        _serviceFactory = serviceFactory;
        _notificationPublisher = notificationPublisher ?? new ForeachAwaitPublisher();
    }

    /// <summary>
    ///     Send a request through the mediator with pipeline behavior support and O(1) handler lookup
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="request">Request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response</returns>
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();

        // Get pipeline behaviors for this request type
        var behaviors = GetPipelineBehaviors<TResponse>(requestType);

        // Build the handler delegate that will be called at the end of the pipeline
        RequestHandlerDelegate<TResponse> handlerDelegate = () => ExecuteHandler(request, requestType, cancellationToken);

        // If no behaviors, execute handler directly
        if (behaviors.Count == 0)
        {
            return await handlerDelegate().ConfigureAwait(false);
        }

        // Build the pipeline chain from innermost to outermost
        // The last behavior wraps the handler, then each preceding behavior wraps the next
        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = handlerDelegate;
            handlerDelegate = () => InvokeBehavior(behavior, request, next, cancellationToken);
        }

        // Execute the pipeline
        return await handlerDelegate().ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets pipeline behaviors for the specified request type
    /// </summary>
    private List<object> GetPipelineBehaviors<TResponse>(Type requestType)
    {
        // O(1) lookup for pipeline behavior type
        var behaviorType = s_pipelineBehaviorTypeCache.GetOrAdd(
            requestType,
            rt => typeof(IPipelineBehavior<,>).MakeGenericType(rt, typeof(TResponse))
        );

        var enumerableType = typeof(IEnumerable<>).MakeGenericType(behaviorType);
        var behaviorsEnumerable = _serviceFactory(enumerableType) as IEnumerable;

        if (behaviorsEnumerable == null)
            return [];

        var behaviors = new List<object>();
        foreach (var behavior in behaviorsEnumerable)
        {
            if (behavior != null)
                behaviors.Add(behavior);
        }

        return behaviors;
    }

    /// <summary>
    ///     Invokes a pipeline behavior with the request and next delegate
    /// </summary>
    private static async Task<TResponse> InvokeBehavior<TResponse>(
        object behavior,
        IRequest<TResponse> request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Use reflection to invoke the Handle method on the behavior
        // This is necessary because we don't know the concrete request type at compile time
        var behaviorType = behavior.GetType();
        var handleMethod = behaviorType.GetMethod("Handle");

        if (handleMethod == null)
            throw new InvalidOperationException($"Pipeline behavior {behaviorType.Name} does not have a Handle method");

        var result = handleMethod.Invoke(behavior, [request, next, cancellationToken]);

        if (result is Task<TResponse> task)
            return await task.ConfigureAwait(false);

        throw new InvalidOperationException($"Pipeline behavior {behaviorType.Name} returned unexpected type: {result?.GetType()}");
    }

    /// <summary>
    ///     Executes the actual request handler
    /// </summary>
    private async Task<TResponse> ExecuteHandler<TResponse>(IRequest<TResponse> request, Type requestType, CancellationToken cancellationToken)
    {
        // O(1) lookup for handler type
        var handlerType = s_handlerTypeCache.GetOrAdd(requestType, static rt => typeof(IRequestHandler<,>).MakeGenericType(rt, typeof(TResponse)));

        var handler = GetHandler(handlerType);

        // O(1) lookup for handler metadata with compiled delegate
        var metadata = s_handlerCache.GetOrAdd(
            handlerType,
            static ht =>
            {
                var method = ht.GetMethod("Handle");

                if (method == null) throw new InvalidOperationException($"Handler method not found for {ht}");

                return new HandlerMetadata { HandlerType = ht, HandleMethod = method, CachedInvoker = CreateCachedInvoker<TResponse>(method) };
            }
        );

        // Cached delegate invocation — wraps reflection for O(1) dispatch after first call
        if (metadata.CachedInvoker != null)
        {
            var result = await metadata.CachedInvoker(handler, [request, cancellationToken]).ConfigureAwait(false);

            return (TResponse) result!;
        }

        // Fallback to reflection (should rarely happen)
        return await HandleRequestFallback<TResponse>(handler, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Send a request through the mediator with pipeline behavior support and O(1) handler lookup
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = typeof(TRequest);

        // Get pipeline behaviors for this request type (Unit response)
        var behaviors = GetPipelineBehaviorsForUnit(requestType);

        // Build the handler delegate that will be called at the end of the pipeline
        RequestHandlerDelegate<Unit> handlerDelegate = () => ExecuteHandlerUnit(request, requestType, cancellationToken);

        // If no behaviors, execute handler directly
        if (behaviors.Count == 0)
        {
            await handlerDelegate().ConfigureAwait(false);
            return;
        }

        // Build the pipeline chain from innermost to outermost
        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var next = handlerDelegate;
            handlerDelegate = () => InvokeBehaviorUnit(behavior, request, next, cancellationToken);
        }

        // Execute the pipeline
        await handlerDelegate().ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets pipeline behaviors for the specified request type with Unit response
    /// </summary>
    private List<object> GetPipelineBehaviorsForUnit(Type requestType)
    {
        var behaviorType = s_pipelineBehaviorTypeCache.GetOrAdd(
            requestType,
            rt => typeof(IPipelineBehavior<,>).MakeGenericType(rt, typeof(Unit))
        );

        var enumerableType = typeof(IEnumerable<>).MakeGenericType(behaviorType);
        var behaviorsEnumerable = _serviceFactory(enumerableType) as IEnumerable;

        if (behaviorsEnumerable == null)
            return [];

        var behaviors = new List<object>();
        foreach (var behavior in behaviorsEnumerable)
        {
            if (behavior != null)
                behaviors.Add(behavior);
        }

        return behaviors;
    }

    /// <summary>
    ///     Invokes a pipeline behavior for Unit response
    /// </summary>
    private static async Task<Unit> InvokeBehaviorUnit<TRequest>(
        object behavior,
        TRequest request,
        RequestHandlerDelegate<Unit> next,
        CancellationToken cancellationToken) where TRequest : IRequest
    {
        var behaviorType = behavior.GetType();
        var handleMethod = behaviorType.GetMethod("Handle");

        if (handleMethod == null)
            throw new InvalidOperationException($"Pipeline behavior {behaviorType.Name} does not have a Handle method");

        var result = handleMethod.Invoke(behavior, [request, next, cancellationToken]);

        if (result is Task<Unit> task)
            return await task.ConfigureAwait(false);

        throw new InvalidOperationException($"Pipeline behavior {behaviorType.Name} returned unexpected type: {result?.GetType()}");
    }

    /// <summary>
    ///     Executes the actual request handler for Unit response
    /// </summary>
    private async Task<Unit> ExecuteHandlerUnit<TRequest>(TRequest request, Type requestType, CancellationToken cancellationToken) where TRequest : IRequest
    {
        // O(1) lookup for handler type - IRequest is IRequest<Unit>
        var handlerType = s_handlerTypeCache.GetOrAdd(requestType, static rt => typeof(IRequestHandler<,>).MakeGenericType(rt, typeof(Unit)));

        var handler = GetHandler(handlerType);

        // O(1) lookup for handler metadata
        var metadata = s_handlerCache.GetOrAdd(
            handlerType,
            static ht =>
            {
                var method = ht.GetMethod("Handle");

                if (method == null) throw new InvalidOperationException($"Handler method not found for {ht}");

                return new HandlerMetadata { HandlerType = ht, HandleMethod = method, CachedInvoker = CreateCachedInvokerUnit(method) };
            }
        );

        // Cached delegate invocation
        if (metadata.CachedInvoker != null)
        {
            await metadata.CachedInvoker(handler, [request, cancellationToken]).ConfigureAwait(false);
            return Unit.Value;
        }

        // Fallback to reflection
        var result = metadata.HandleMethod.Invoke(handler, [request, cancellationToken]);

        if (result is Task<Unit> task)
        {
            await task.ConfigureAwait(false);
            return Unit.Value;
        }

        throw new InvalidOperationException($"Handler returned unexpected type: {result?.GetType()}");
    }

    /// <summary>
    ///     Send a request through the mediator with optimized interface scanning
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response</returns>
    public async Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();

        // Optimized interface scanning - O(n) instead of LINQ
        Type? targetInterface = null;
        var interfaces = requestType.GetInterfaces();

        for (var i = 0; i < interfaces.Length; i++)
        {
            var @interface = interfaces[i];

            if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IRequest<>))
            {
                targetInterface = @interface;

                break; // Take first match
            }
        }

        if (targetInterface != null)
        {
            var responseType = targetInterface.GetGenericArguments()[0];
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
            var handler = _serviceFactory(handlerType);

            if (handler != null)
            {
                var method = handlerType.GetMethod("Handle");

                if (method != null)
                {
                    var result = method.Invoke(handler, [request, cancellationToken]);

                    if (result is Task task)
                    {
                        await task.ConfigureAwait(false);

                        if (task.GetType().IsGenericType) { return task.GetType().GetProperty("Result")?.GetValue(task); }
                    }
                }
            }
        }

        // Try IRequest (Unit response) 
        if (request is IRequest)
        {
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(Unit));
            var handler = _serviceFactory(handlerType);

            if (handler != null)
            {
                var method = handlerType.GetMethod("Handle");

                if (method != null)
                {
                    var result = method.Invoke(handler, [request, cancellationToken]);

                    if (result is Task<Unit> unitTask) { return await unitTask.ConfigureAwait(false); }
                }
            }
        }

        throw new InvalidOperationException($"No handler found for request type {requestType}");
    }

    /// <summary>
    ///     Publish a notification through the mediator with optimized O(n) enumeration
    /// </summary>
    /// <typeparam name="TNotification">Notification type</typeparam>
    /// <param name="notification">Notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        if (notification is null) throw new ArgumentNullException(nameof(notification));

        var handlerType = typeof(INotificationHandler<TNotification>);
        var handlers = _serviceFactory(typeof(IEnumerable<>).MakeGenericType(handlerType)) as IEnumerable<INotificationHandler<TNotification>>;

        if (handlers != null)
        {
            // Convert to array once for efficient enumeration - O(n)
            var handlerArray = handlers as INotificationHandler<TNotification>[ ] ?? handlers.ToArray();

            if (handlerArray.Length > 0)
            {
                // Pre-allocate array for better performance - O(1) allocation  
                var executors = new NotificationHandlerExecutorImpl<TNotification>[handlerArray.Length];

                // Create executors - O(n)
                for (var i = 0; i < handlerArray.Length; i++) { executors[i] = new NotificationHandlerExecutorImpl<TNotification>(handlerArray[i]); }

                await _notificationPublisher.Publish(executors, notification, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    ///     Publish a notification through the mediator
    /// </summary>
    /// <param name="notification">Notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public async Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (notification is INotification notificationInstance)
        {
            var notificationType = notification.GetType();
            var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(handlerType);
            var handlers = _serviceFactory(enumerableType) as IEnumerable;

            if (handlers != null)
            {
                var executors = new List<NotificationHandlerExecutor>();

                foreach (var handler in handlers)
                {
                    var executorType = typeof(NotificationHandlerExecutorImpl<>).MakeGenericType(notificationType);
                    var executor = Activator.CreateInstance(executorType, handler) as NotificationHandlerExecutor;

                    if (executor != null) { executors.Add(executor); }
                }

                if (executors.Count > 0) { await _notificationPublisher.Publish(executors, notificationInstance, cancellationToken).ConfigureAwait(false); }
            }
        }
        else { throw new InvalidOperationException($"Object {notification.GetType()} does not implement INotification"); }
    }

    /// <summary>
    ///     Create a stream for the request
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="request">Request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response stream</returns>
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStream<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return CreateStreamCore(request, cancellationToken);
    }

    private async IAsyncEnumerable<TResponse> CreateStreamCore<TResponse>(IStream<TResponse> request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        var handlerType = typeof(IStreamRequestHandler<,>).MakeGenericType(requestType, responseType);
        var handler = GetHandler(handlerType);

        var method = handlerType.GetMethod("Handle");

        if (method == null) throw new InvalidOperationException($"Handler method not found for {handlerType}");

        var result = method.Invoke(handler, [request, cancellationToken]);

        if (result is IAsyncEnumerable<TResponse> stream)
        {
            await foreach (var item in stream.WithCancellation(cancellationToken).ConfigureAwait(false)) { yield return item; }
        }
        else { throw new InvalidOperationException($"Handler returned unexpected type: {result?.GetType()}"); }
    }

    private object GetHandler(Type handlerType)
    {
        var handler = _serviceFactory(handlerType);

        if (handler == null) throw new InvalidOperationException($"Handler not found for {handlerType}");

        return handler;
    }

    /// <summary>
    ///     Creates a cached delegate wrapping <c>MethodInfo.Invoke</c> for handler dispatch.
    ///     The delegate is created once per handler type and reused for all subsequent calls.
    ///     Note: This still uses reflection internally — it is NOT a compiled expression tree.
    /// </summary>
    private static Func<object, object[], Task<object?>>? CreateCachedInvoker<TResponse>(MethodInfo method)
    {
        try
        {
            // For async methods returning Task<TResponse>
            return async (handler, args) =>
            {
                var result = method.Invoke(handler, args);

                if (result is Task<TResponse> task)
                {
                    var response = await task.ConfigureAwait(false);

                    return response;
                }

                throw new InvalidOperationException($"Handler returned unexpected type: {result?.GetType()}");
            };
        }
        catch (ArgumentException)
        {
            return null; // Invalid method signature - fallback to reflection
        }
        catch (NotSupportedException)
        {
            return null; // Method type not supported - fallback to reflection
        }
    }

    /// <summary>
    ///     Creates a cached delegate wrapping <c>MethodInfo.Invoke</c> for Unit-returning handlers.
    ///     Note: This still uses reflection internally — it is NOT a compiled expression tree.
    /// </summary>
    private static Func<object, object[], Task<object?>>? CreateCachedInvokerUnit(MethodInfo method)
    {
        try
        {
            return async (handler, args) =>
            {
                var result = method.Invoke(handler, args);

                if (result is Task<Unit> task)
                {
                    await task.ConfigureAwait(false);

                    return Unit.Value;
                }

                throw new InvalidOperationException($"Handler returned unexpected type: {result?.GetType()}");
            };
        }
        catch (ArgumentException)
        {
            return null; // Invalid method signature - fallback to reflection
        }
        catch (NotSupportedException)
        {
            return null; // Method type not supported - fallback to reflection
        }
    }

    /// <summary>
    ///     Fallback method using reflection when compiled delegates fail
    /// </summary>
    private static async Task<TResponse> HandleRequestFallback<TResponse>(object handler, object request, CancellationToken cancellationToken)
    {
        var method = handler.GetType().GetMethod("Handle");

        if (method == null) throw new InvalidOperationException($"Handler method not found for {handler.GetType()}");

        var result = method.Invoke(handler, [request, cancellationToken]);

        if (result is Task<TResponse> task) return await task.ConfigureAwait(false);

        throw new InvalidOperationException($"Handler returned unexpected type: {result?.GetType()}");
    }
}
