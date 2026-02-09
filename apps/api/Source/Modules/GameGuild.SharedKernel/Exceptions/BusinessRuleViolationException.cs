namespace GameGuild;

/// <summary>
///     Exception thrown when business rules are violated
/// </summary>
public class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string rule, string message, object? context = null) : base(message)
    {
        Rule = rule;
        Context = context;
    }

    public BusinessRuleViolationException(string rule, string message, Exception innerException, object? context = null) : base(message, innerException)
    {
        Rule = rule;
        Context = context;
    }

    /// <summary>Name or identifier of the business rule that was violated.</summary>
    public string Rule { get; }

    /// <summary>Optional contextual data related to the violation (e.g., the entity or input that triggered it).</summary>
    public object? Context { get; }
}
