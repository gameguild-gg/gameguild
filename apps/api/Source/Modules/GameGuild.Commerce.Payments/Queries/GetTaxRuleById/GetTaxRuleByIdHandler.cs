using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for GetTaxRuleByIdQuery
/// </summary>
public sealed class GetTaxRuleByIdHandler : IQueryHandler<GetTaxRuleByIdQuery, TaxRuleDto?>
{
    public Task<TaxRuleDto?> Handle(GetTaxRuleByIdQuery request, CancellationToken cancellationToken)
    {
        // Placeholder implementation - would query actual rule data
        return Task.FromResult<TaxRuleDto?>(null);
    }
}
