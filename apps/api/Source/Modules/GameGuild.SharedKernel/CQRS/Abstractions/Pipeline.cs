namespace GameGuild.CQRS;

/// <summary>
///     Represents a request handler pipeline behavior
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequestBase
{
    /// <summary>
    ///     Pipeline behavior handler
    /// </summary>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

/// <summary>
///     Defines a request pre-processor
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
public interface IRequestPreProcessor<in TRequest> where TRequest : IRequestBase
{
    /// <summary>
    ///     Process method executed before the handler
    /// </summary>
    Task Process(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
///     Defines a request post-processor
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IRequestPostProcessor<in TRequest, in TResponse> where TRequest : IRequestBase
{
    /// <summary>
    ///     Process method executed after the handler
    /// </summary>
    Task Process(TRequest request, TResponse response, CancellationToken cancellationToken);
}

/// <summary>
///     Defines an exception handler for requests
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
/// <typeparam name="TException">Exception type</typeparam>
public interface IRequestExceptionHandler<in TRequest, TResponse, in TException> where TRequest : IRequestBase where TException : Exception
{
    /// <summary>
    ///     Handles exceptions during request processing.
    ///     Set <paramref name="state"/>.State to <see cref="RequestExceptionHandlerState.Handled"/>
    ///     to signal that the exception was handled and the returned response should be used.
    /// </summary>
    Task<TResponse> Handle(TRequest request, TException exception, RequestExceptionHandlerStateWrapper state, CancellationToken cancellationToken);
}

/// <summary>
///     Defines a generic exception action for requests
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TException">Exception type</typeparam>
public interface IRequestExceptionAction<in TRequest, in TException> where TRequest : IRequestBase where TException : Exception
{
    /// <summary>
    ///     Executes action when exception occurs
    /// </summary>
    Task Execute(TRequest request, TException exception, CancellationToken cancellationToken);
}

/// <summary>
///     State for request exception handling
/// </summary>
public enum RequestExceptionHandlerState
{
    /// <summary>
    ///     Continue to next exception handler
    /// </summary>
    Continue,

    /// <summary>
    ///     Stop processing and return response
    /// </summary>
    Handled
}

/// <summary>
///     Mutable wrapper around <see cref="RequestExceptionHandlerState"/> so that exception handlers
///     can signal whether the exception was handled. Enums are value types and cannot communicate
///     state changes back to the caller when passed as parameters.
/// </summary>
public sealed class RequestExceptionHandlerStateWrapper
{
    /// <summary>
    ///     Gets or sets the current handler state.
    /// </summary>
    public RequestExceptionHandlerState State { get; set; } = RequestExceptionHandlerState.Continue;
}
