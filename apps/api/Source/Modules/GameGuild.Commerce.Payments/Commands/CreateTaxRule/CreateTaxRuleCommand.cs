using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Command to create a tax rule
/// </summary>
public sealed record CreateTaxRuleCommand(
    string JurisdictionCode,
    string? ProductCategory,
    string CustomerType,
    decimal Rate,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string? Description) : ICommand<Guid>;
