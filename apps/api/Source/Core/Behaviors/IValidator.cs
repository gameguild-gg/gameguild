namespace GameGuild.Core.Behaviors;

/// <summary> Simple validator interface using modern Result pattern </summary>
/// <typeparam name="T"> Type to validate </typeparam>
public interface IValidator<T> {
  /// <summary> Validates the instance using FluentValidation context </summary>
  /// <param name="context"> FluentValidation context </param>
  /// <param name="cancellationToken"> Cancellation token </param>
  /// <returns> Result indicating success or validation failure </returns>
  Task<Result> ValidateAsync(FluentValidation.ValidationContext<T> context, CancellationToken cancellationToken = default);
}
