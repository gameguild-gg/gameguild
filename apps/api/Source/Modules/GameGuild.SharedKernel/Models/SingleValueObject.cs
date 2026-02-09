namespace GameGuild;

/// <summary>
///     Base record for immutable value objects wrapping a single value.
///     As a <c>record</c>, equality is derived from declared properties automatically.
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
    ///     Explicit conversion to the wrapped type.
    ///     Uses explicit (not implicit) to preserve type safety — implicit conversion silently
    ///     strips the domain meaning from the value object.
    /// </summary>
    /// <param name="valueObject">The value object to convert</param>
    /// <returns>The wrapped value</returns>
    public static explicit operator T(SingleValueObject<T> valueObject) { return valueObject.Value; }
}
