namespace GameGuild.Abstractions;

/// <summary>
///     Base record class for immutable value objects that follow DDD principles.
///     Value objects are immutable objects that are identified by their content rather than identity.
/// </summary>
public abstract record ValueObject
{
    /// <summary>
    ///     Returns a string representation of the value object.
    ///     Override this method to provide a meaningful string representation.
    /// </summary>
    /// <returns>A string that represents the value object</returns>
    public override string ToString() { return $"{GetType().Name} {{ {string.Join(", ", GetEqualityComponents().Select(x => x?.ToString() ?? "null"))} }}"; }

    /// <summary>
    ///     Gets the components that determine equality for this value object.
    ///     Implement this method to return all properties that should be considered for equality.
    /// </summary>
    /// <returns>An enumerable of objects that determine equality</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();
}
