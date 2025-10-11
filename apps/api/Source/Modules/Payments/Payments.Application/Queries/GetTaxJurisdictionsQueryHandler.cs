using GameGuild.Modules.Payments.Domain.Entities;
using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.CQRS;

namespace GameGuild.Modules.Payments.Queries;

/// <summary>
///     Handler for GetTaxJurisdictionsQuery
/// </summary>
public class GetTaxJurisdictionsQueryHandler : IRequestHandler<GetTaxJurisdictionsQuery, IEnumerable<TaxJurisdiction>>
{
    private readonly ITaxCalculationService _taxCalculationService;

    public GetTaxJurisdictionsQueryHandler(ITaxCalculationService taxCalculationService)
    {
        _taxCalculationService = taxCalculationService;
    }

    public async Task<IEnumerable<TaxJurisdiction>> Handle(GetTaxJurisdictionsQuery request, CancellationToken cancellationToken)
    {
        return await _taxCalculationService.GetTaxJurisdictionsAsync(cancellationToken);
    }
}
