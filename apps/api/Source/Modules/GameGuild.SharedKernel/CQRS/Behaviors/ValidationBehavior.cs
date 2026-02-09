using FluentValidation;
using FluentValidation.Results;

namespace GameGuild.CQRS;

/// <summary>
///     Pipeline behavior for validating requests using FluentValidation validators.
///     Validators are executed sequentially to avoid issues with scoped services (e.g., DbContext)
///     that are not thread-safe.
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequestBase
{
    private readonly FluentValidation.IValidator<TRequest>[] _validators;

    /// <summary>
    ///     Initializes a new instance of the ValidationBehavior class
    /// </summary>
    /// <param name="validators">Collection of FluentValidation validators</param>
    public ValidationBehavior(IEnumerable<FluentValidation.IValidator<TRequest>> validators) { _validators = validators as FluentValidation.IValidator<TRequest>[] ?? validators.ToArray(); }

    /// <summary>
    ///     Handles the request pipeline with validation.
    ///     Validators run sequentially (not Task.WhenAll) because FluentValidation validators
    ///     may depend on scoped services like DbContext that are not thread-safe.
    /// </summary>
    /// <param name="request">Request</param>
    /// <param name="next">Next handler delegate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (_validators.Length > 0)
        {
            var context = new FluentValidation.ValidationContext<TRequest>(request);
            var failures = new List<ValidationFailure>();

            foreach (var validator in _validators)
            {
                var result = await validator.ValidateAsync(context, cancellationToken).ConfigureAwait(false);
                if (!result.IsValid)
                {
                    failures.AddRange(result.Errors);
                }
            }

            if (failures.Count > 0)
            {
                throw new RequestValidationException(
                    failures.Select(f => new ValidationError(f.PropertyName, f.ErrorMessage, f.AttemptedValue)));
            }
        }

        return await next().ConfigureAwait(false);
    }
}
