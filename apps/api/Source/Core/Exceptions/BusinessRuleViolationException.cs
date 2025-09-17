namespace GameGuild;

/// <summary> Exception thrown when business rules are violated </summary>
public class BusinessRuleViolationException : DomainException {
  public BusinessRuleViolationException(string rule, string message, object? context = null) : base(message) {
    Rule = rule;
    Context = context;
  }

  public BusinessRuleViolationException(string rule, string message, Exception innerException, object? context = null) : base(message, innerException) {
    Rule = rule;
    Context = context;
  }

  public string Rule { get; }

  public object? Context { get; }
}
