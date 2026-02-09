using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get tax rule by ID
/// </summary>
public sealed record GetTaxRuleByIdQuery(Guid RuleId) : IQuery<TaxRuleDto?>;
