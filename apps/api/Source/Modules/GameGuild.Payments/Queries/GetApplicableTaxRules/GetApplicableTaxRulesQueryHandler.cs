using GameGuild.CQRS;
using GameGuild.Payments.Abstractions;
using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Handler for GetApplicableTaxRulesQuery
/// </summary>
public sealed class GetApplicableTaxRulesQueryHandler(ITaxCalculationService taxCalculationService) : IQueryHandler<GetApplicableTaxRulesQuery, List<TaxRule>>
{
    public async Task<List<TaxRule>> Handle(GetApplicableTaxRulesQuery request, CancellationToken cancellationToken)
    {
        var rules = await taxCalculationService.GetApplicableTaxRulesAsync(request.JurisdictionCode, request.CustomerType, request.EffectiveDate, cancellationToken);

        return rules.ToList();
    }
}
