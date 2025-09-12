namespace GameGuild.CQRS;

/// <summary>
///    Defines an exception handler for requests
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
/// <typeparam name="TException">Exception type</typeparam>
public interface IRequestExceptionHandler<in TRequest, TResponse, in TException>
    where TRequest : IBaseRequest
    where TException : Exception
{
    /// <summary>
    ///    Handles exceptions during request processing
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="exception">Exception</param>
    /// <param name="state">Current state</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response or re-throws exception</returns>
    Task<TResponse> Handle(TRequest request, TException exception, RequestExceptionHandlerState state, CancellationToken cancellationToken);
}
