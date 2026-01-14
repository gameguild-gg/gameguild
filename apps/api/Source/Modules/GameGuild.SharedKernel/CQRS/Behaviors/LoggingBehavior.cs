using Microsoft.Extensions.Logging;

namespace GameGuild.CQRS;

/// <summary>
///     Pipeline behavior for logging requests and responses
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequestBase
{

    /// <summary>
    ///     Handles the request pipeline
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="next">Next handler delegate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;
        var requestGuid = Guid.NewGuid();

        logger.LogInformation("Handling request {RequestName} {RequestId}", requestName, requestGuid);

        try
        {
            var response = await next().ConfigureAwait(false);
            logger.LogInformation("Request {RequestName} {RequestId} handled successfully", requestName, requestGuid);

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Request {RequestName} {RequestId} failed", requestName, requestGuid);

            throw;
        }
    }
}
