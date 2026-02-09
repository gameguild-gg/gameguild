namespace GameGuild;

/// <summary>
///     Base record for immutable value objects that follow DDD principles.
///     Value objects are immutable and identified by their content rather than identity.
///     As a <c>record</c>, structural equality is provided automatically by the compiler —
///     derived records simply declare properties and get correct <c>Equals</c> / <c>GetHashCode</c> for free.
/// </summary>
public abstract record ValueObject;
