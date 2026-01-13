using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get applicable tax rules
/// </summary>
public sealed record GetApplicableTaxRulesQuery(string JurisdictionCode, CustomerType CustomerType, DateTime? EffectiveDate = null) : IQuery<List<TaxRule>>;
