using GameGuild.CQRS;

namespace GameGuild;

/// <summary>
/// Simplified validation behavior that converts FluentValidation failures to Result<T> with Error.
/// No more exceptions for business logic - only Result<Error> patterns.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> {

  private readonly IEnumerable<FluentValidation.IValidator<TRequest>> _validators;
  private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

  public ValidationBehavior(
      IEnumerable<FluentValidation.IValidator<TRequest>> validators,
      ILogger<ValidationBehavior<TRequest, TResponse>> logger) {
    _validators = validators;
    _logger = logger;
  }

  public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegateBase<TResponse> next, CancellationToken cancellationToken) {
    ArgumentNullException.ThrowIfNull(next);

    var requestName = typeof(TRequest).Name;
    _logger.LogDebug("Validating request {RequestName}", requestName);

    if (!_validators.Any()) {
      _logger.LogDebug("No validators found for {RequestName}", requestName);
      return await next().ConfigureAwait(false);
    }

    var context = new FluentValidation.ValidationContext<TRequest>(request);
    var validationResults = await Task.WhenAll(
        _validators.Select(v => v.ValidateAsync(context, cancellationToken))
    ).ConfigureAwait(false);

    var failures = validationResults
        .Where(r => !r.IsValid)
        .SelectMany(r => r.Errors)
        .ToArray();

    if (failures.Length == 0) {
      _logger.LogDebug("Validation passed for {RequestName}", requestName);
      return await next().ConfigureAwait(false);
    }

    _logger.LogWarning("Validation failed for {RequestName} with {ErrorCount} errors",
        requestName, failures.Length);

    // Convert FluentValidation failures to our unified Error format
    var errors = failures.Select(f =>
        Error.ValidationFailure(f.PropertyName, f.ErrorMessage, f.AttemptedValue)
    ).ToArray();

    // Handle Result<T> responses - preferred approach
    if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>)) {
      var valueType = typeof(TResponse).GetGenericArguments()[0];
      var failureMethod = typeof(Result).GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!.MakeGenericMethod(valueType);

      // Use the first error for the Result, but log all errors
      var primaryError = errors.First();
      return (TResponse)failureMethod.Invoke(null, [primaryError])!;
    }

    // Handle non-generic Result responses  
    if (typeof(TResponse) == typeof(Result)) {
      return (TResponse)(object)Result.Failure(errors.First());
    }

    // For non-Result types, we cannot return validation errors properly
    // Log this as an error since all new code should use Result<T>
    _logger.LogError("Cannot handle validation errors for non-Result response type: {ResponseType}. Please update to use Result<T>", typeof(TResponse).Name);
    var errorMessage = string.Join("; ", errors.Select(e => $"{e.GetProperty()}: {e.Message}"));
    throw new InvalidOperationException($"Validation failed but cannot return Result: {errorMessage}");
  }
}
