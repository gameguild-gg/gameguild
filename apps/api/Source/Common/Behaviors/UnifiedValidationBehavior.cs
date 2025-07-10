using System.ComponentModel.DataAnnotations;
using FluentValidation;
using MediatR;


namespace GameGuild.Common;

/// <summary>
/// Unified validation behavior that supports both DataAnnotations and FluentValidation
/// </summary>
public class UnifiedValidationBehavior<TRequest, TResponse>(
  IEnumerable<IValidator<TRequest>> fluentValidators,
  ILogger<UnifiedValidationBehavior<TRequest, TResponse>> logger
)
  : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse> {
  public async Task<TResponse> Handle(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken
  ) {
    var requestName = typeof(TRequest).Name;
    logger.LogDebug("Validating {RequestName}", requestName);

    // Collect all validation errors
    var validationErrors = new List<string>();

    // 1. DataAnnotations validation (for backward compatibility)
    var dataAnnotationErrors = ValidateWithDataAnnotations(request);
    validationErrors.AddRange(dataAnnotationErrors);

    // 2. FluentValidation (preferred approach)
    var fluentValidationErrors = await ValidateWithFluentValidation(request, cancellationToken);
    validationErrors.AddRange(fluentValidationErrors);

    // If there are validation errors, handle them appropriately
    if (validationErrors.Any()) {
      var errorMessage = string.Join("; ", validationErrors);
      logger.LogWarning(
        "Validation failed for {RequestName}: {ValidationErrors}",
        requestName,
        errorMessage
      );

      // Handle Result pattern responses
      if (typeof(TResponse).IsGenericType &&
          typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>)) {
        var resultType = typeof(TResponse).GetGenericArguments()[0];
        var error = Error.Failure("Validation.Failed", errorMessage);
        var failureMethod = typeof(Result).GetMethod("Failure", [typeof(Error)])!
                                          .MakeGenericMethod(resultType);

        return (TResponse)failureMethod.Invoke(null, [error])!;
      }

      if (typeof(TResponse) == typeof(Result)) {
        var error = Error.Failure("Validation.Failed", errorMessage);

        return (TResponse)(object)Result.Failure(error);
      }

      // Fallback to exception for non-Result responses
      throw new ValidationException(errorMessage);
    }

    logger.LogDebug("Validation passed for {RequestName}", requestName);

    return await next();
  }

  private static List<string> ValidateWithDataAnnotations(TRequest request) {
    var validationContext = new ValidationContext(request);
    var validationResults = new List<ValidationResult>();
    var errors = new List<string>();

    var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

    if (!isValid)
      errors.AddRange(
        validationResults
          .Where(r => !string.IsNullOrEmpty(r.ErrorMessage))
          .Select(r => r.ErrorMessage!)
      );

    return errors;
  }

  private async Task<List<string>> ValidateWithFluentValidation(TRequest request, CancellationToken cancellationToken) {
    var errors = new List<string>();

    if (!fluentValidators.Any()) return errors;

    var context = new ValidationContext<TRequest>(request);

    var validationResults = await Task.WhenAll(
                              fluentValidators.Select(validator => validator.ValidateAsync(context, cancellationToken))
                            );

    var validationFailures = validationResults
                             .Where(result => !result.IsValid)
                             .SelectMany(result => result.Errors)
                             .ToArray();

    errors.AddRange(
      validationFailures.Select(failure =>
                                  $"{failure.PropertyName}: {failure.ErrorMessage}"
      )
    );

    return errors;
  }
}
