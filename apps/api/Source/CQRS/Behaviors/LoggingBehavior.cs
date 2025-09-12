namespace GameGuild.CQRS;

/// <summary>
/// Pipeline behavior for logging requests and responses
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    ///  Initializes a new instance of the LoggingBehavior class
    /// </summary>
    /// <param name="logger">Logger</param>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) { _logger = logger; }

    /// <summary>
    ///  Handles the request pipeline
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

        _logger.LogInformation("Handling request {RequestName} {RequestId}", requestName, requestGuid);

        try
        {
            var response = await next().ConfigureAwait(false);
            _logger.LogInformation("Request {RequestName} {RequestId} handled successfully", requestName, requestGuid);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request {RequestName} {RequestId} failed", requestName, requestGuid);

            throw;
        }
    }
}
