using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for GetTaxJurisdictionsQuery
/// </summary>
public sealed class GetTaxJurisdictionsQueryHandler(ITaxCalculationService taxCalculationService) : IQueryHandler<GetTaxJurisdictionsQuery, List<TaxJurisdiction>>
{
    public async Task<List<TaxJurisdiction>> Handle(GetTaxJurisdictionsQuery request, CancellationToken cancellationToken)
    {
        var jurisdictions = await taxCalculationService.GetTaxJurisdictionsAsync(cancellationToken);

        return jurisdictions.ToList();
    }
}
