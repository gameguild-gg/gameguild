using GameGuild.Modules.Payments.Entities;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Queries;

/// <summary>
///     Get all tax jurisdictions
/// </summary>
public record GetTaxJurisdictionsQuery : IRequest<IEnumerable<TaxJurisdiction>>;
