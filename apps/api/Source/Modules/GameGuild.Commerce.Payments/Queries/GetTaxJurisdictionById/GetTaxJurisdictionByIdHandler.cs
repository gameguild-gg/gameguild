using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for GetTaxJurisdictionByIdQuery
/// </summary>
public sealed class GetTaxJurisdictionByIdHandler : IQueryHandler<GetTaxJurisdictionByIdQuery, TaxJurisdictionDto?>
{
    public Task<TaxJurisdictionDto?> Handle(GetTaxJurisdictionByIdQuery request, CancellationToken cancellationToken)
    {
        // Placeholder implementation - would query actual jurisdiction data
        return Task.FromResult<TaxJurisdictionDto?>(null);
    }
}
