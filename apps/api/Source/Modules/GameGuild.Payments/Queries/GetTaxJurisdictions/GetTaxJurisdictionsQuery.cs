using GameGuild.CQRS;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to get all tax jurisdictions
/// </summary>
public sealed record GetTaxJurisdictionsQuery : IQuery<List<TaxJurisdiction>>;
