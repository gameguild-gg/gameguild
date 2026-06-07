namespace GameGuild;

/// <summary>
///     An aggregate validation error that wraps one or more individual <see cref="Error" /> instances.
///     Used by the Result pattern to represent compound validation failures.
///     Not to be confused with <see cref="CQRS.ValidationError" />, which represents a single field-level validation error.
/// </summary>
public sealed record AggregateValidationError(Error[] Errors)
    : Error("Validation.General", "One or more validation errors occurred", ErrorType.Validation)
{
    public static AggregateValidationError FromResults(IEnumerable<Result> results)
        => new(results.Where(r => r.IsFailure).Select(r => r.Error).ToArray());
}
