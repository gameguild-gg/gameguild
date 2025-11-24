namespace GameGuild.Abstractions;

/// <summary>
///     Base record class for immutable value objects with a single value.
///     Use this for simple value objects that wrap a single primitive value.
/// </summary>
/// <typeparam name="T">The type of the wrapped value</typeparam>
public abstract record SingleValueObject<T> : ValueObject
{
    /// <summary>
    ///     Initializes a new instance of the SingleValueObject class.
    /// </summary>
    /// <param name="value">The value to wrap</param>
    protected SingleValueObject(T value) { Value = value; }

    /// <summary>
    ///     The wrapped value.
    /// </summary>
    public T Value { get; init; }

    /// <summary>
    ///     Gets the equality components for this single value object.
    /// </summary>
    /// <returns>The wrapped value as the only equality component</returns>
    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }

    /// <summary>
    ///     Implicit conversion to the wrapped type.
    /// </summary>
    /// <param name="valueObject">The value object to convert</param>
    /// <returns>The wrapped value</returns>
    public static implicit operator T(SingleValueObject<T> valueObject) { return valueObject.Value; }
}
