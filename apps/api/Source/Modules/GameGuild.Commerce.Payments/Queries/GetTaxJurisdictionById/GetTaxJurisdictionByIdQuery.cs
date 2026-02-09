using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to get tax jurisdiction by ID
/// </summary>
public sealed record GetTaxJurisdictionByIdQuery(Guid JurisdictionId) : IQuery<TaxJurisdictionDto?>;
