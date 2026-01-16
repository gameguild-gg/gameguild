using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get tax rule by ID
/// </summary>
public record GetTaxRuleByIdQuery(Guid RuleId) : IQuery<TaxRuleDto?>;
