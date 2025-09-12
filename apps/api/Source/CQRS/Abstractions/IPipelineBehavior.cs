namespace GameGuild.CQRS;

/// <summary>
/// Represents a request handler pipeline behavior
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IBaseRequest
{
    /// <summary>
    /// Pipeline behavior handler
    /// </summary>
    /// <param name="request">Request instance</param>
    /// <param name="next">Next delegate to call in pipeline</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response from handler</returns>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
