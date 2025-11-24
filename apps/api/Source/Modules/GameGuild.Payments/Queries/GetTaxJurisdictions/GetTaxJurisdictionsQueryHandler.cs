using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

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
