namespace GameGuild.CQRS;

/// <summary>
/// Enhanced pipeline behavior for validating requests that returns Result<T> instead of throwing exceptions
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class UnifiedValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of the UnifiedValidationBehavior class
    /// </summary>
    /// <param name="validators">Collection of validators</param>
    public UnifiedValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>
    /// Handles the request pipeline with validation, returning Result<T> for failed validation
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="next">Next handler delegate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response or validation failure result</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegateBase<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                    _validators.Select(v => v.ValidateAsync(context, cancellationToken))
                )
                .ConfigureAwait(false);

            var failures = validationResults
                .Where(r => !r.IsValid)
                .SelectMany(r => r.Errors)
                .ToArray();

            if (failures.Length != 0)
            {
                // If TResponse is a Result or Result<T>, return a validation failure result
                if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var valueType = typeof(TResponse).GetGenericArguments()[0];
                    var validationFailureMethod = typeof(Result)
                        .GetMethod(nameof(Result.ValidationFailure), [typeof(ValidationError[])])!
                        .MakeGenericMethod(valueType);

                    return (TResponse)validationFailureMethod.Invoke(null, [failures])!;
                }
                else if (typeof(TResponse) == typeof(Result))
                {
                    return (TResponse)(object)Result.ValidationFailure(failures);
                }
                else
                {
                    // Fallback to throwing exception for non-Result responses (backward compatibility)
                    throw new ValidationException(failures);
                }
            }
        }

        return await next().ConfigureAwait(false);
    }
}
