namespace GameGuild.CQRS;

/// <summary>
///     Pipeline behavior for request pre-processing
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public sealed class RequestPreProcessorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequestBase
{
    private readonly IRequestPreProcessor<TRequest>[] _preProcessors;

    /// <summary>
    ///     Initializes a new instance of the RequestPreProcessorBehavior class
    /// </summary>
    /// <param name="preProcessors">Pre-processors</param>
    public RequestPreProcessorBehavior(IEnumerable<IRequestPreProcessor<TRequest>> preProcessors) { _preProcessors = preProcessors as IRequestPreProcessor<TRequest>[] ?? preProcessors.ToArray(); }

    /// <summary>
    ///     Handles the pipeline behavior
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="next">Next delegate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        foreach (var processor in _preProcessors) { await processor.Process(request, cancellationToken).ConfigureAwait(false); }

        return await next().ConfigureAwait(false);
    }
}
