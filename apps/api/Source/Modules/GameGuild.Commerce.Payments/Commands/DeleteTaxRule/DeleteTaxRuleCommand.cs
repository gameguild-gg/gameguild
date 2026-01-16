using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to delete a tax rule
/// </summary>
public record DeleteTaxRuleCommand(Guid RuleId) : ICommand;
