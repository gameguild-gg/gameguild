using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get all tax jurisdictions
/// </summary>
public sealed record GetTaxJurisdictionsQuery : IQuery<List<TaxJurisdiction>>;
