namespace GameGuild.CQRS;

/// <summary>
///     Pipeline behavior for request post-processing
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public sealed class RequestPostProcessorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequestBase
{
    private readonly IRequestPostProcessor<TRequest, TResponse>[] _postProcessors;

    /// <summary>
    ///     Initializes a new instance of the RequestPostProcessorBehavior class
    /// </summary>
    /// <param name="postProcessors">Post-processors</param>
    public RequestPostProcessorBehavior(IEnumerable<IRequestPostProcessor<TRequest, TResponse>> postProcessors) { _postProcessors = postProcessors as IRequestPostProcessor<TRequest, TResponse>[] ?? postProcessors.ToArray(); }

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

        var response = await next().ConfigureAwait(false);

        foreach (var processor in _postProcessors) { await processor.Process(request, response, cancellationToken).ConfigureAwait(false); }

        return response;
    }
}
