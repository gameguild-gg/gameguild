using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for GetApplicableTaxRulesQuery
/// </summary>
public sealed class GetApplicableTaxRulesQueryHandler(ITaxCalculationService taxCalculationService) : IQueryHandler<GetApplicableTaxRulesQuery, List<TaxRule>>
{
    public async Task<List<TaxRule>> Handle(GetApplicableTaxRulesQuery request, CancellationToken cancellationToken)
    {
        var rules = await taxCalculationService.GetApplicableTaxRulesAsync(request.JurisdictionCode, request.CustomerType, request.EffectiveDate, cancellationToken).ConfigureAwait(false);

        return rules.ToList();
    }
}
