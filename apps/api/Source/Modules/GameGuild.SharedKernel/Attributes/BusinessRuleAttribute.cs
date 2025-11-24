using GameGuild.Exceptions;

namespace GameGuild.Resources.Attributes;

/// <summary>
///     Business rule validation attribute for domain methods
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter)]
public class BusinessRuleAttribute(string rule, string? description = null, BusinessRuleSeverity severity = BusinessRuleSeverity.Error) : Attribute
{
    public string Rule { get; } = rule;

    public string? Description { get; } = description;

    public BusinessRuleSeverity Severity { get; } = severity;
}
