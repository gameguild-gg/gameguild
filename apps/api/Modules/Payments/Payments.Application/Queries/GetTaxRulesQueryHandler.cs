using GameGuild.Modules.Payments.Domain.Entities;
using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Queries;

/// <summary>
///     Handler for GetTaxRulesQuery
/// </summary>
public class GetTaxRulesQueryHandler : IRequestHandler<GetTaxRulesQuery, IEnumerable<TaxRule>>
{
    private readonly ITaxCalculationService _taxCalculationService;

    public GetTaxRulesQueryHandler(ITaxCalculationService taxCalculationService)
    {
        _taxCalculationService = taxCalculationService;
    }

    public async Task<IEnumerable<TaxRule>> Handle(GetTaxRulesQuery request, CancellationToken cancellationToken)
    {
        var customerType = string.IsNullOrEmpty(request.CustomerType)
            ? CustomerType.B2C
            : Enum.Parse<CustomerType>(request.CustomerType, ignoreCase: true);

        return await _taxCalculationService.GetApplicableTaxRulesAsync(
            request.JurisdictionCode,
            customerType,
            request.EffectiveDate,
            cancellationToken);
    }
}
