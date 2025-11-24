using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to get applicable tax rules
/// </summary>
public sealed record GetApplicableTaxRulesQuery(string JurisdictionCode, CustomerType CustomerType, DateTime? EffectiveDate = null) : IQuery<List<TaxRule>>;
