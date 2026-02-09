using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to partially update a tax rule
/// </summary>
public sealed record PatchTaxRuleCommand(
    Guid RuleId,
    decimal? Rate,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    string? Description,
    bool? IsActive) : ICommand;
