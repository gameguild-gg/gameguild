using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GameGuild.CQRS;

/// <summary>
/// Optimized mediator implementation with O(1) handler lookup and compiled delegates
/// </summary>
public class Mediator : IMediator
{
    // O(1) lookup caches for performance
    private static readonly ConcurrentDictionary<Type, HandlerMetadata> _handlerCache = new ConcurrentDictionary<Type, HandlerMetadata>();

    private static readonly ConcurrentDictionary<Type, Type> _handlerTypeCache = new ConcurrentDictionary<Type, Type>();

    private readonly INotificationPublisher _notificationPublisher;

    private readonly ServiceFactory _serviceFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator" /> class.
    /// </summary>
    /// <param name="serviceFactory">Service factory</param>
    /// <param name="notificationPublisher">Notification publisher</param>
    public Mediator(ServiceFactory serviceFactory, INotificationPublisher? notificationPublisher = null)
    {
        _serviceFactory = serviceFactory;
        _notificationPublisher = notificationPublisher ?? new ForeachAwaitPublisher();
    }

    /// <summary>
    /// Send a request through the mediator with O(1) handler lookup
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="request">Request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response</returns>
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();

        // O(1) lookup for handler type
        var handlerType = _handlerTypeCache.GetOrAdd(
            requestType,
            static rt =>
                typeof(IRequestHandler<,>).MakeGenericType(rt, typeof(TResponse))
        );

        var handler = GetHandler(handlerType);

        // O(1) lookup for handler metadata with compiled delegate
        var metadata = _handlerCache.GetOrAdd(
            handlerType,
            static ht =>
            {
                var method = ht.GetMethod("Handle");

                if (method == null) throw new InvalidOperationException($"Handler method not found for {ht}");

                return new HandlerMetadata
                {
                    HandlerType = ht, HandleMethod = method, CompiledInvoker = CreateCompiledInvoker<TResponse>(method)
                };
            }
        );

        // Fast compiled delegate invocation instead of reflection
        if (metadata.CompiledInvoker != null)
        {
            var result = await metadata.CompiledInvoker(handler, [request, cancellationToken]).ConfigureAwait(false);

            return (TResponse)result!;
        }

        // Fallback to reflection (should rarely happen)
        return await HandleRequestFallback<TResponse>(handler, request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a request through the mediator with O(1) handler lookup
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response</returns>
    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = typeof(TRequest);

        // O(1) lookup for handler type - IRequest is IRequest<Unit>
        var handlerType = _handlerTypeCache.GetOrAdd(
            requestType,
            static rt =>
                typeof(IRequestHandler<,>).MakeGenericType(rt, typeof(Unit))
        );

        var handler = GetHandler(handlerType);

        // O(1) lookup for handler metadata
        var metadata = _handlerCache.GetOrAdd(
            handlerType,
            static ht =>
            {
                var method = ht.GetMethod("Handle");

                if (method == null) throw new InvalidOperationException($"Handler method not found for {ht}");

                return new HandlerMetadata
                {
                    HandlerType = ht, HandleMethod = method, CompiledInvoker = CreateCompiledInvokerUnit(method)
                };
            }
        );

        // Fast compiled delegate invocation
        if (metadata.CompiledInvoker != null)
        {
            await metadata.CompiledInvoker(handler, [request, cancellationToken]).ConfigureAwait(false);

            return;
        }

        // Fallback to reflection
        var result = metadata.HandleMethod.Invoke(handler, [request, cancellationToken]);
        if (result is Task<Unit> task)
        {
            await task.ConfigureAwait(false);
        }
        else
        {
            throw new InvalidOperationException($"Handler returned unexpected type: {result?.GetType()}");
        }
    }

    /// <summary>
    /// Send a request through the mediator with optimized interface scanning
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
                        if (task.GetType().IsGenericType)
                        {
                            return task.GetType().GetProperty("Result")?.GetValue(task);
                        }
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
                    if (result is Task<Unit> unitTask)
                    {
                        return await unitTask.ConfigureAwait(false);
                    }
                }
            }
        }

        throw new InvalidOperationException($"No handler found for request type {requestType}");
    }

    /// <summary>
    /// Publish a notification through the mediator with optimized O(n) enumeration
    /// </summary>
    /// <typeparam name="TNotification">Notification type</typeparam>
    /// <param name="notification">Notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        if (notification is null) throw new ArgumentNullException(nameof(notification));

        var handlerType = typeof(INotificationHandler<TNotification>);
        var handlers = _serviceFactory(typeof(IEnumerable<>).MakeGenericType(handlerType)) as IEnumerable<INotificationHandler<TNotification>>;

        if (handlers != null)
        {
            // Convert to array once for efficient enumeration - O(n)
            var handlerArray = handlers as INotificationHandler<TNotification>[] ?? handlers.ToArray();

            if (handlerArray.Length > 0)
            {
                // Pre-allocate array for better performance - O(1) allocation  
                var executors = new NotificationHandlerExecutor<TNotification>[handlerArray.Length];

                // Create executors - O(n)
                for (var i = 0; i < handlerArray.Length; i++)
                {
                    executors[i] = new NotificationHandlerExecutor<TNotification>(handlerArray[i]);
                }

                await _notificationPublisher.Publish(executors, notification, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Publish a notification through the mediator
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
                var executors = new List<NotificationHandlerExecutorBase>();
                foreach (var handler in handlers)
                {
                    var executorType = typeof(NotificationHandlerExecutor<>).MakeGenericType(notificationType);
                    var executor = Activator.CreateInstance(executorType, handler) as NotificationHandlerExecutorBase;
                    if (executor != null)
                    {
                        executors.Add(executor);
                    }
                }

                if (executors.Count > 0)
                {
                    await _notificationPublisher.Publish(executors, notificationInstance, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        else
        {
            throw new InvalidOperationException($"Object {notification.GetType()} does not implement INotification");
        }
    }

    /// <summary>
    /// Create a stream for the request
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="request">Request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response stream</returns>
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return CreateStreamCore(request, cancellationToken);
    }

    private async IAsyncEnumerable<TResponse> CreateStreamCore<TResponse>(IStreamRequest<TResponse> request, [EnumeratorCancellation] CancellationToken cancellationToken)
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
            await foreach (var item in stream.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }
        else
        {
            throw new InvalidOperationException($"Handler returned unexpected type: {result?.GetType()}");
        }
    }

    private object GetHandler(Type handlerType)
    {
        var handler = _serviceFactory(handlerType);

        if (handler == null) throw new InvalidOperationException($"Handler not found for {handlerType}");

        return handler;
    }

    /// <summary>
    /// Creates a compiled delegate for fast method invocation - O(1) after compilation
    /// </summary>
    private static Func<object, object[], Task<object?>>? CreateCompiledInvoker<TResponse>(MethodInfo method)
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
    /// Creates a compiled delegate for Unit returns - O(1) after compilation
    /// </summary>
    private static Func<object, object[], Task<object?>>? CreateCompiledInvokerUnit(MethodInfo method)
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
    /// Fallback method using reflection when compiled delegates fail
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
